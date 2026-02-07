using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

#if !BROWSER
using NAudio.Lame;
using NAudio.Wave;
#endif

namespace Insait_Translator_Deutsch.Services;

#if BROWSER
/// <summary>
/// Browser build stub. Real TTS/MP3 is handled by the native app and accessed via HTTP.
/// </summary>
public class TextToSpeechService : IDisposable
{
    public bool IsInitialized => true;

    public Task InitializeAsync(Action<string>? statusCallback = null)
    {
        statusCallback?.Invoke("TTS доступний лише в нативному додатку");
        return Task.CompletedTask;
    }

    public Task SpeakAsync(string text, Action<string>? statusCallback = null)
    {
        statusCallback?.Invoke("TTS доступний лише в нативному додатку");
        return Task.CompletedTask;
    }

    public Task SaveToMp3Async(string text, string filePath, Action<string>? statusCallback = null)
    {
        statusCallback?.Invoke("MP3 генерується в нативному додатку");
        return Task.CompletedTask;
    }

    public void Dispose() { }
}

#else

public class TextToSpeechService : IDisposable
{
    private bool _isInitialized;
    private readonly string _basePath;
    private readonly string _piperExePath;
    private readonly string _modelPath;
    private readonly string _modelConfigPath;
    private bool _disposed;
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromMinutes(15) };

    // Piper release URLs
    private const string PIPER_WINDOWS_URL = "https://github.com/rhasspy/piper/releases/download/2023.11.14-2/piper_windows_amd64.zip";
    
    // German thorsten voice model (high quality)
    private const string MODEL_URL = "https://huggingface.co/rhasspy/piper-voices/resolve/main/de/de_DE/thorsten/high/de_DE-thorsten-high.onnx";
    private const string MODEL_CONFIG_URL = "https://huggingface.co/rhasspy/piper-voices/resolve/main/de/de_DE/thorsten/high/de_DE-thorsten-high.onnx.json";

    public bool IsInitialized => _isInitialized;

    public TextToSpeechService()
    {
        _basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "InsaitTranslator");
        
        var piperDir = Path.Combine(_basePath, "piper");
        _piperExePath = Path.Combine(piperDir, "piper", "piper.exe");
        
        var modelsDir = Path.Combine(_basePath, "models");
        _modelPath = Path.Combine(modelsDir, "de_DE-thorsten-high.onnx");
        _modelConfigPath = Path.Combine(modelsDir, "de_DE-thorsten-high.onnx.json");
        
        Directory.CreateDirectory(piperDir);
        Directory.CreateDirectory(modelsDir);
    }

    public async Task InitializeAsync(Action<string>? statusCallback = null)
    {
        if (_isInitialized)
            return;

        try
        {
            // Check if Windows
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new PlatformNotSupportedException("Piper TTS наразі підтримується лише на Windows");
            }

            // Download Piper executable if not exists
            if (!File.Exists(_piperExePath))
            {
                statusCallback?.Invoke("Завантаження Piper TTS (~15 MB)...");
                
                var zipPath = Path.Combine(_basePath, "piper.zip");
                
                using var response = await _httpClient.GetAsync(PIPER_WINDOWS_URL, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                
                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                
                await using (var contentStream = await response.Content.ReadAsStreamAsync())
                await using (var fileStream = File.Create(zipPath))
                {
                    var buffer = new byte[81920];
                    long totalRead = 0;
                    int bytesRead;
                    
                    while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                        totalRead += bytesRead;
                        
                        if (totalBytes > 0)
                        {
                            var progress = (int)(totalRead * 100 / totalBytes);
                            statusCallback?.Invoke($"Завантаження Piper TTS... {progress}%");
                        }
                    }
                }

                statusCallback?.Invoke("Розпаковка Piper...");
                var extractPath = Path.Combine(_basePath, "piper");
                if (Directory.Exists(extractPath))
                {
                    Directory.Delete(extractPath, true);
                }
                ZipFile.ExtractToDirectory(zipPath, extractPath);
                File.Delete(zipPath);
            }

            // Download German voice model if not exists
            if (!File.Exists(_modelPath))
            {
                statusCallback?.Invoke("Завантаження німецької моделі голосу (~65 MB)...");
                
                using var modelResponse = await _httpClient.GetAsync(MODEL_URL, HttpCompletionOption.ResponseHeadersRead);
                modelResponse.EnsureSuccessStatusCode();
                
                var totalBytes = modelResponse.Content.Headers.ContentLength ?? 0;
                
                await using (var contentStream = await modelResponse.Content.ReadAsStreamAsync())
                await using (var fileStream = File.Create(_modelPath))
                {
                    var buffer = new byte[81920];
                    long totalRead = 0;
                    int bytesRead;
                    
                    while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                        totalRead += bytesRead;
                        
                        if (totalBytes > 0)
                        {
                            var progress = (int)(totalRead * 100 / totalBytes);
                            statusCallback?.Invoke($"Завантаження моделі голосу... {progress}%");
                        }
                    }
                }
            }

            // Download model config if not exists
            if (!File.Exists(_modelConfigPath))
            {
                statusCallback?.Invoke("Завантаження конфігурації моделі...");
                
                using var configResponse = await _httpClient.GetAsync(MODEL_CONFIG_URL);
                configResponse.EnsureSuccessStatusCode();
                
                var configContent = await configResponse.Content.ReadAsStringAsync();
                await File.WriteAllTextAsync(_modelConfigPath, configContent);
            }

            // Verify piper executable exists
            if (!File.Exists(_piperExePath))
            {
                throw new FileNotFoundException($"Piper executable не знайдено: {_piperExePath}");
            }

            _isInitialized = true;
            statusCallback?.Invoke("TTS готовий");
        }
        catch (Exception ex)
        {
            statusCallback?.Invoke($"Помилка ініціалізації TTS: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Synthesize text to WAV audio bytes
    /// </summary>
    public async Task<byte[]> SynthesizeToWavAsync(string text)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("TTS не ініціалізовано");
        }

        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<byte>();

        var outputPath = Path.Combine(_basePath, $"output_{Guid.NewGuid()}.wav");

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _piperExePath,
                Arguments = $"--model \"{_modelPath}\" --output_file \"{outputPath}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(_piperExePath)
            };

            using var process = new Process();
            process.StartInfo = startInfo;
            process.Start();

            // Send text to Piper via stdin
            await process.StandardInput.WriteLineAsync(text);
            process.StandardInput.Close();

            // Wait for process with timeout
            var completed = await Task.Run(() => process.WaitForExit(60000));
            
            if (!completed)
            {
                process.Kill();
                throw new TimeoutException("Piper TTS timeout");
            }

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"Piper error (code {process.ExitCode}): {error}");
            }

            // Read generated audio
            if (!File.Exists(outputPath))
            {
                throw new FileNotFoundException("Piper не створив аудіо файл");
            }
            
            var audioData = await File.ReadAllBytesAsync(outputPath);
            return audioData;
        }
        finally
        {
            // Cleanup temp file
            if (File.Exists(outputPath))
            {
                try { File.Delete(outputPath); } catch { /* ignore cleanup errors */ }
            }
        }
    }

    /// <summary>
    /// Play German text using system audio
    /// </summary>
    public async Task SpeakAsync(string text, Action<string>? statusCallback = null)
    {
        if (!_isInitialized)
        {
            await InitializeAsync(statusCallback);
        }

        if (string.IsNullOrWhiteSpace(text))
            return;

        statusCallback?.Invoke("Генерація мовлення...");

        var audioData = await SynthesizeToWavAsync(text);
        
        if (audioData.Length == 0)
        {
            statusCallback?.Invoke("Помилка: порожнє аудіо");
            return;
        }

        statusCallback?.Invoke("Відтворення...");

        // Play WAV audio using NAudio
        using var ms = new MemoryStream(audioData);
        using var reader = new WaveFileReader(ms);
        using var outputDevice = new WaveOutEvent();
        
        outputDevice.Init(reader);
        outputDevice.Play();
        
        while (outputDevice.PlaybackState == PlaybackState.Playing)
        {
            await Task.Delay(100);
        }

        statusCallback?.Invoke("Готово");
    }

    /// <summary>
    /// Save German text as MP3 file
    /// </summary>
    public async Task SaveToMp3Async(string text, string filePath, Action<string>? statusCallback = null)
    {
        if (!_isInitialized)
        {
            await InitializeAsync(statusCallback);
        }

        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Текст не може бути порожнім", nameof(text));

        statusCallback?.Invoke("Генерація аудіо з тексту...");

        // Generate WAV from text using Piper
        var audioData = await SynthesizeToWavAsync(text);
        
        if (audioData.Length == 0)
            throw new InvalidOperationException("Не вдалося згенерувати аудіо");

        statusCallback?.Invoke("Конвертація в MP3...");

        // Convert WAV to MP3 using LAME
        using var wavStream = new MemoryStream(audioData);
        using var reader = new WaveFileReader(wavStream);
        
        // Create MP3 file
        await using var mp3Writer = new LameMP3FileWriter(filePath, reader.WaveFormat, LAMEPreset.STANDARD);
        
        var buffer = new byte[reader.WaveFormat.AverageBytesPerSecond];
        int bytesRead;
        while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
        {
            await mp3Writer.WriteAsync(buffer.AsMemory(0, bytesRead));
        }

        statusCallback?.Invoke($"Збережено: {Path.GetFileName(filePath)}");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}

#endif
