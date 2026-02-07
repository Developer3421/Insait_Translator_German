using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Insait_Translator_Deutsch.Localization;

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
        statusCallback?.Invoke(LocalizationManager.Instance.Strings.TtsAvailableOnlyInNative);
        return Task.CompletedTask;
    }

    public Task SpeakAsync(string text, Action<string>? statusCallback = null)
    {
        statusCallback?.Invoke(LocalizationManager.Instance.Strings.TtsAvailableOnlyInNative);
        return Task.CompletedTask;
    }

    public Task SaveToMp3Async(string text, string filePath, Action<string>? statusCallback = null)
    {
        statusCallback?.Invoke(LocalizationManager.Instance.Strings.Mp3GeneratedInNative);
        return Task.CompletedTask;
    }

    public void Dispose() { }
}

#else

public class TextToSpeechService : IDisposable
{
    private bool _isInitialized;
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
        // All paths are managed by AppDataPaths for Microsoft Store compatibility
        // Directories are created lazily when accessed
    }

    public async Task InitializeAsync(Action<string>? statusCallback = null)
    {
        if (_isInitialized)
            return;

        try
        {
            var strings = LocalizationManager.Instance.Strings;
            
            // Check if Windows
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new PlatformNotSupportedException(strings.TtsPlatformNotSupported);
            }

            // Download Piper executable if not exists
            if (!File.Exists(AppDataPaths.PiperExePath))
            {
                statusCallback?.Invoke(strings.TtsDownloadingPiper);
                
                var zipPath = Path.Combine(AppDataPaths.PiperDirectory, "piper.zip");
                
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
                            statusCallback?.Invoke(string.Format(strings.TtsDownloadingPiperProgress, progress));
                        }
                    }
                }

                statusCallback?.Invoke(strings.TtsExtractingPiper);
                var extractPath = AppDataPaths.PiperDirectory;
                if (Directory.Exists(Path.Combine(extractPath, "piper")))
                {
                    Directory.Delete(Path.Combine(extractPath, "piper"), true);
                }
                ZipFile.ExtractToDirectory(zipPath, extractPath);
                File.Delete(zipPath);
            }

            // Download German voice model if not exists
            if (!File.Exists(AppDataPaths.ThorstenModelPath))
            {
                statusCallback?.Invoke(strings.TtsDownloadingVoiceModel);
                
                using var modelResponse = await _httpClient.GetAsync(MODEL_URL, HttpCompletionOption.ResponseHeadersRead);
                modelResponse.EnsureSuccessStatusCode();
                
                var totalBytes = modelResponse.Content.Headers.ContentLength ?? 0;
                
                await using (var contentStream = await modelResponse.Content.ReadAsStreamAsync())
                await using (var fileStream = File.Create(AppDataPaths.ThorstenModelPath))
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
                            statusCallback?.Invoke(string.Format(strings.TtsDownloadingVoiceModelProgress, progress));
                        }
                    }
                }
            }

            // Download model config if not exists
            if (!File.Exists(AppDataPaths.ThorstenModelConfigPath))
            {
                statusCallback?.Invoke(strings.TtsDownloadingModelConfig);
                
                using var configResponse = await _httpClient.GetAsync(MODEL_CONFIG_URL);
                configResponse.EnsureSuccessStatusCode();
                
                var configContent = await configResponse.Content.ReadAsStringAsync();
                await File.WriteAllTextAsync(AppDataPaths.ThorstenModelConfigPath, configContent);
            }

            // Verify piper executable exists
            if (!File.Exists(AppDataPaths.PiperExePath))
            {
                throw new FileNotFoundException(string.Format(strings.TtsPiperNotFound, AppDataPaths.PiperExePath));
            }

            _isInitialized = true;
            statusCallback?.Invoke(strings.TtsReady);
        }
        catch (Exception ex)
        {
            var strings = LocalizationManager.Instance.Strings;
            statusCallback?.Invoke(string.Format(strings.TtsInitError, ex.Message));
            throw;
        }
    }

    /// <summary>
    /// Synthesize text to WAV audio bytes
    /// </summary>
    public async Task<byte[]> SynthesizeToWavAsync(string text)
    {
        var strings = LocalizationManager.Instance.Strings;
        
        if (!_isInitialized)
        {
            throw new InvalidOperationException(strings.TtsNotInitialized);
        }

        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<byte>();

        var outputPath = AppDataPaths.GetTempFilePath(".wav");

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = AppDataPaths.PiperExePath,
                Arguments = $"--model \"{AppDataPaths.ThorstenModelPath}\" --output_file \"{outputPath}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(AppDataPaths.PiperExePath)
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
                throw new FileNotFoundException(strings.TtsAudioFileNotCreated);
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

        var strings = LocalizationManager.Instance.Strings;
        statusCallback?.Invoke(strings.TtsGeneratingSpeech);

        var audioData = await SynthesizeToWavAsync(text);
        
        if (audioData.Length == 0)
        {
            statusCallback?.Invoke(strings.TtsEmptyAudioError);
            return;
        }

        statusCallback?.Invoke(strings.TtsPlaying);

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

        statusCallback?.Invoke(strings.Ready);
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

        var strings = LocalizationManager.Instance.Strings;

        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException(strings.TtsTextCannotBeEmpty, nameof(text));

        statusCallback?.Invoke(strings.TtsGeneratingAudio);

        // Generate WAV from text using Piper
        var audioData = await SynthesizeToWavAsync(text);
        
        if (audioData.Length == 0)
            throw new InvalidOperationException(strings.TtsCouldNotGenerateAudio);

        statusCallback?.Invoke(strings.TtsConvertingToMp3);

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

        statusCallback?.Invoke(string.Format(strings.TtsSavedAs, Path.GetFileName(filePath)));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}

#endif
