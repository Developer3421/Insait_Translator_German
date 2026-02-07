using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using GTranslate.Translators;

namespace Insait_Translator_Deutsch.Services;

/// <summary>
/// Translation provider types
/// </summary>
public enum TranslationProvider
{
    Auto,       // MyMemory -> GTranslate fallback
    MyMemory,   // Free API with limits
    GTranslate, // Google Translate (unofficial)
    GoogleApi   // Google Cloud Translation API (requires key)
}

/// <summary>
/// Translation result with provider info
/// </summary>
public class TranslationResult
{
    public string Text { get; set; } = string.Empty;
    public TranslationProvider UsedProvider { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public bool WasFallback { get; set; }
}

public class TranslationService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly GoogleTranslator _googleTranslator;
    private bool _myMemoryExhausted;
    private DateTime _myMemoryExhaustedTime;

    public TranslationProvider CurrentProvider { get; private set; } = TranslationProvider.Auto;
    public event EventHandler<string>? ProviderChanged;

    public TranslationService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _googleTranslator = new GoogleTranslator();
    }

    /// <summary>
    /// Translates text using the configured provider with automatic fallback
    /// </summary>
    public async Task<TranslationResult> TranslateWithDetailsAsync(string text, string sourceLang = "uk", string targetLang = "de")
    {
        if (string.IsNullOrWhiteSpace(text))
            return new TranslationResult { Text = string.Empty };

        var settings = SettingsService.Instance;
        
        // Check if Google API is preferred and has API key
        if (settings.UseGoogleApi && !string.IsNullOrEmpty(settings.GoogleApiKey))
        {
            // Don't fallback - throw error so user knows Gemini API failed
            var result = await TranslateWithGoogleApiAsync(text, sourceLang, targetLang);
            return new TranslationResult
            {
                Text = result,
                UsedProvider = TranslationProvider.GoogleApi,
                ProviderName = "Google Gemini API"
            };
        }

        // Reset MyMemory exhausted flag after 1 hour
        if (_myMemoryExhausted && (DateTime.Now - _myMemoryExhaustedTime).TotalHours >= 1)
        {
            _myMemoryExhausted = false;
        }

        // Try MyMemory first (if not exhausted)
        if (!_myMemoryExhausted)
        {
            try
            {
                var result = await TranslateWithMyMemoryAsync(text, sourceLang, targetLang);
                CurrentProvider = TranslationProvider.MyMemory;
                return new TranslationResult
                {
                    Text = result,
                    UsedProvider = TranslationProvider.MyMemory,
                    ProviderName = "MyMemory"
                };
            }
            catch (MyMemoryQuotaExceededException)
            {
                _myMemoryExhausted = true;
                _myMemoryExhaustedTime = DateTime.Now;
                ProviderChanged?.Invoke(this, "GTranslate (MyMemory вичерпано)");
            }
            catch (Exception)
            {
                // Try fallback
            }
        }

        // Fallback to GTranslate
        try
        {
            var result = await TranslateWithGTranslateAsync(text, sourceLang, targetLang);
            CurrentProvider = TranslationProvider.GTranslate;
            return new TranslationResult
            {
                Text = result,
                UsedProvider = TranslationProvider.GTranslate,
                ProviderName = "Google Translate",
                WasFallback = true
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Помилка перекладу: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Simple translation method for backward compatibility
    /// </summary>
    public async Task<string> TranslateAsync(string text, string sourceLang = "uk", string targetLang = "de")
    {
        var result = await TranslateWithDetailsAsync(text, sourceLang, targetLang);
        return result.Text;
    }

    /// <summary>
    /// Translates using MyMemory API (free, with daily limit)
    /// </summary>
    private async Task<string> TranslateWithMyMemoryAsync(string text, string sourceLang, string targetLang)
    {
        var encodedText = HttpUtility.UrlEncode(text);
        
        // MyMemory supports "autodetect" for auto-detection
        var effectiveSourceLang = string.IsNullOrEmpty(sourceLang) || sourceLang.Equals("auto", StringComparison.OrdinalIgnoreCase) 
            ? "autodetect" 
            : sourceLang;
        
        var url = $"https://api.mymemory.translated.net/get?q={encodedText}&langpair={effectiveSourceLang}|{targetLang}";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var responseData = doc.RootElement.GetProperty("responseData");
        var translatedText = responseData.GetProperty("translatedText").GetString();

        var responseStatus = doc.RootElement.GetProperty("responseStatus").GetInt32();
        
        // Check for quota exceeded
        if (responseStatus == 429 || 
            (doc.RootElement.TryGetProperty("quotaFinished", out var quotaFinished) && quotaFinished.GetBoolean()))
        {
            throw new MyMemoryQuotaExceededException("MyMemory daily quota exceeded");
        }

        if (responseStatus != 200)
        {
            throw new Exception($"MyMemory API error: status {responseStatus}");
        }

        // Check for "PLEASE SELECT TWO DISTINCT LANGUAGES" or similar errors
        if (translatedText?.Contains("MYMEMORY WARNING") == true ||
            translatedText?.Contains("QUOTA") == true)
        {
            throw new MyMemoryQuotaExceededException("MyMemory quota warning detected");
        }

        return translatedText ?? string.Empty;
    }

    /// <summary>
    /// Translates using GTranslate (Google Translate unofficial)
    /// </summary>
    private async Task<string> TranslateWithGTranslateAsync(string text, string sourceLang, string targetLang)
    {
        // GTranslate doesn't support "auto" - use null for auto-detection
        if (string.IsNullOrEmpty(sourceLang) || sourceLang.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            var result = await _googleTranslator.TranslateAsync(text, targetLang);
            return result.Translation;
        }
        else
        {
            var result = await _googleTranslator.TranslateAsync(text, targetLang, sourceLang);
            return result.Translation;
        }
    }

    /// <summary>
    /// Translates using Google Gemini API (requires API key)
    /// </summary>
    private async Task<string> TranslateWithGoogleApiAsync(string text, string sourceLang, string targetLang)
    {
        var apiKey = SettingsService.Instance.GoogleApiKey;
        if (string.IsNullOrEmpty(apiKey))
            throw new Exception("Google API key not configured");

        var targetLanguageName = GetLanguageName(targetLang);

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

        string prompt;
        if (string.IsNullOrEmpty(sourceLang) || sourceLang.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            // Auto-detect source language
            prompt = $"Translate the following text to {targetLanguageName}. Return ONLY the translation, nothing else - no explanations, no quotes, no additional text:\n\n{text}";
        }
        else
        {
            var sourceLanguageName = GetLanguageName(sourceLang);
            prompt = $"Translate the following text from {sourceLanguageName} to {targetLanguageName}. Return ONLY the translation, nothing else - no explanations, no quotes, no additional text:\n\n{text}";
        }

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.1,
                maxOutputTokens = 8192
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Google Gemini API error: {response.StatusCode} - {errorContent}");
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var candidates = doc.RootElement.GetProperty("candidates");
        if (candidates.GetArrayLength() > 0)
        {
            var content = candidates[0].GetProperty("content");
            var parts = content.GetProperty("parts");
            if (parts.GetArrayLength() > 0)
            {
                var translatedText = parts[0].GetProperty("text").GetString() ?? string.Empty;
                return translatedText.Trim();
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Gets the full language name from language code
    /// </summary>
    private static string GetLanguageName(string langCode)
    {
        return langCode.ToLower() switch
        {
            "uk" => "Ukrainian",
            "de" => "German",
            "en" => "English",
            "ru" => "Russian",
            "pl" => "Polish",
            "fr" => "French",
            "es" => "Spanish",
            "it" => "Italian",
            _ => langCode
        };
    }

    /// <summary>
    /// Tests the Google Gemini API key
    /// </summary>
    public async Task<(bool Success, string Message)> TestGoogleApiKeyAsync(string apiKey)
    {
        try
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = "Translate to German. Return ONLY the translation: Hello" }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.1,
                    maxOutputTokens = 100
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                return (true, "API ключ дійсний! Google Gemini API працює.");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return (false, $"Помилка: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Помилка підключення: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

/// <summary>
/// Exception thrown when MyMemory quota is exceeded
/// </summary>
public class MyMemoryQuotaExceededException : Exception
{
    public MyMemoryQuotaExceededException(string message) : base(message) { }
}

