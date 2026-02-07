using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace Insait_Translator_Deutsch.Services;

public class TranslationService : IDisposable
{
    private readonly HttpClient _httpClient;

    public TranslationService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Translates text from Ukrainian to German using MyMemory API (free, no API key required)
    /// </summary>
    public async Task<string> TranslateAsync(string text, string sourceLang = "uk", string targetLang = "de")
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        try
        {
            // MyMemory Translation API - free, no API key needed
            // Limit: 1000 chars per request, 10000 chars/day for anonymous users
            var encodedText = HttpUtility.UrlEncode(text);
            var url = $"https://api.mymemory.translated.net/get?q={encodedText}&langpair={sourceLang}|{targetLang}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var responseData = doc.RootElement.GetProperty("responseData");
            var translatedText = responseData.GetProperty("translatedText").GetString();

            // Check for errors in response
            var responseStatus = doc.RootElement.GetProperty("responseStatus").GetInt32();
            if (responseStatus != 200)
            {
                throw new Exception($"Translation API error: status {responseStatus}");
            }

            return translatedText ?? string.Empty;
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Помилка з'єднання: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            throw new Exception($"Помилка обробки відповіді: {ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

