using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using InsaitTranslatorGerman.Localization;
using LibVLCSharp.Shared;
using NAudio.Wave;
namespace InsaitTranslatorGerman.Services;
/// <summary>
/// Text-to-Speech service using Piper TTS for German voice synthesis.
/// Uses bundled Piper files from the application directory.
/// MP3 encoding uses LibVLCSharp for transcoding.
/// </summary>
public class TextToSpeechService : ITextToSpeechService
{
    private bool _isInitialized;
    private bool _disposed;
    public bool IsInitialized => _isInitialized;
    /// <summary>
    /// Initialize Piper TTS - verifies bundled files exist
    /// </summary>
    public async Task InitializeAsync(Action<string>? statusCallback = null)
    {
        if (_isInitialized)
            return;
        var strings = LocalizationManager.Instance.Strings;
        try
        {
            // Check if Windows
            if (!OperatingSystem.IsWindows())
            {
                statusCallback?.Invoke(strings.TtsPlatformNotSupported);
                return;
            }
            statusCallback?.Invoke(strings.TtsDownloadingPiper);
            // Check if Piper is bundled with the application
            await Task.Run(() => VerifyPiperFiles(statusCallback));
            _isInitialized = true;
            statusCallback?.Invoke(strings.TtsReady);
            System.Diagnostics.Debug.WriteLine($"[TTS] Piper TTS initialized. Path: {AppDataPaths.PiperExePath}");
            System.Diagnostics.Debug.WriteLine($"[TTS] Model path: {AppDataPaths.ThorstenModelPath}");
            System.Diagnostics.Debug.WriteLine($"[TTS] Is bundled: {AppDataPaths.IsPiperBundled}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TTS] Initialization error: {ex.Message}");
            statusCallback?.Invoke(string.Format(strings.TtsInitError, ex.Message));
            throw;
        }
    }
    /// <summary>
    /// Verify that Piper TTS files exist (bundled or in AppData)
    /// </summary>
    private void VerifyPiperFiles(Action<string>? statusCallback)
    {
        var strings = LocalizationManager.Instance.Strings;
        // Log paths for debugging
        System.Diagnostics.Debug.WriteLine($"[TTS] App directory: {AppDataPaths.AppDirectory}");
        System.Diagnostics.Debug.WriteLine($"[TTS] Bundled Piper directory: {AppDataPaths.BundledPiperDirectory}");
        System.Diagnostics.Debug.WriteLine($"[TTS] Bundled Piper exe: {AppDataPaths.BundledPiperExePath}");
        System.Diagnostics.Debug.WriteLine($"[TTS] Bundled model: {AppDataPaths.BundledThorstenModelPath}");
        // Check piper.exe
        if (!File.Exists(AppDataPaths.PiperExePath))
        {
            throw new FileNotFoundException(
                string.Format(strings.TtsPiperNotFound, AppDataPaths.PiperExePath));
        }
        // Check voice model
        if (!File.Exists(AppDataPaths.ThorstenModelPath))
        {
            throw new FileNotFoundException(
                $"Voice model not found: {AppDataPaths.ThorstenModelPath}");
        }
        // Check model config
        if (!File.Exists(AppDataPaths.ThorstenModelConfigPath))
        {
            throw new FileNotFoundException(
                $"Voice model config not found: {AppDataPaths.ThorstenModelConfigPath}");
        }
        statusCallback?.Invoke(strings.TtsExtractingPiper);
    }
    /// <summary>
    /// Synthesize text to WAV audio bytes using Piper
    /// </summary>
    public async Task<byte[]> SynthesizeToWavAsync(string text)
    {
        var strings = LocalizationManager.Instance.Strings;
        if (!_isInitialized)
            throw new InvalidOperationException(strings.TtsNotInitialized);
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<byte>();
        // Use system temp path with unique name to avoid conflicts
        var tempWavPath = Path.Combine(Path.GetTempPath(), $"tts_wav_{Guid.NewGuid()}.wav");
        try
        {
            // Run Piper TTS
            var processInfo = new ProcessStartInfo
            {
                FileName = AppDataPaths.PiperExePath,
                Arguments = $"--model \"{AppDataPaths.ThorstenModelPath}\" " +
                           $"--config \"{AppDataPaths.ThorstenModelConfigPath}\" " +
                           $"--output_file \"{tempWavPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = AppDataPaths.PiperDirectory
            };
            using var process = new Process { StartInfo = processInfo };
            process.Start();
            // Write text to stdin
            await process.StandardInput.WriteAsync(text);
            process.StandardInput.Close();
            // Read stdout/stderr to prevent buffer deadlock
            _ = process.StandardOutput.ReadToEndAsync(); // Discard stdout, only needed to prevent deadlock
            var stderrTask = process.StandardError.ReadToEndAsync();
            // Wait for completion (with timeout)
            var completed = await Task.Run(() => process.WaitForExit(60000));
            if (!completed)
            {
                try { process.Kill(); } catch { }
                throw new TimeoutException("Piper TTS timed out");
            }
            // Ensure process has fully exited
            await Task.Run(() => process.WaitForExit());
            // Get stderr for logging
            var error = await stderrTask;
            if (!string.IsNullOrEmpty(error))
            {
                System.Diagnostics.Debug.WriteLine($"[TTS] Piper stderr: {error}");
            }
            // Small delay to ensure file handle is fully released by Piper
            await Task.Delay(100);
            // Read WAV file with retry for file locking issues
            byte[] wavData = await ReadFileWithRetryAsync(tempWavPath, strings.TtsAudioFileNotCreated);
            System.Diagnostics.Debug.WriteLine($"[TTS] WAV file read successfully, size: {wavData.Length} bytes");
            return wavData;
        }
        finally
        {
            // Cleanup temp file with small delay
            await Task.Delay(50);
            await DeleteFileWithRetryAsync(tempWavPath, maxRetries: 2, throwOnError: false);
        }
    }
    /// <summary>
    /// Read file with retry logic for file locking issues
    /// </summary>
    private async Task<byte[]> ReadFileWithRetryAsync(string filePath, string notFoundMessage, int maxRetries = 5)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    if (attempt == maxRetries)
                        throw new InvalidOperationException(notFoundMessage);
                    // File might not be written yet, wait and retry
                    await Task.Delay(100 * attempt);
                    continue;
                }
                return await File.ReadAllBytesAsync(filePath);
            }
            catch (IOException ex) when (attempt < maxRetries)
            {
                System.Diagnostics.Debug.WriteLine($"[TTS] Read attempt {attempt} failed: {ex.Message}");
                await Task.Delay(100 * attempt);
            }
        }
        throw new IOException($"Failed to read file after {maxRetries} attempts: {filePath}");
    }
    /// <summary>
    /// Play German text using system audio
    /// </summary>
    public async Task SpeakAsync(string text, Action<string>? statusCallback = null)
    {
        if (!_isInitialized)
            await InitializeAsync(statusCallback);
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
        using var ms = new MemoryStream(audioData);
        using var reader = new WaveFileReader(ms);
        using var outputDevice = new WaveOutEvent();
        outputDevice.Init(reader);
        outputDevice.Play();
        while (outputDevice.PlaybackState == PlaybackState.Playing)
            await Task.Delay(100);
        statusCallback?.Invoke(strings.Ready);
    }
    /// <summary>
    /// Save German text as MP3 file
    /// </summary>
    public async Task SaveToMp3Async(string text, string filePath, Action<string>? statusCallback = null)
    {
        if (!_isInitialized)
            await InitializeAsync(statusCallback);
        var strings = LocalizationManager.Instance.Strings;
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException(strings.TtsTextCannotBeEmpty, nameof(text));
        statusCallback?.Invoke(strings.TtsGeneratingAudio);
        byte[] audioData;
        try
        {
            audioData = await SynthesizeToWavAsync(text);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TTS] Error synthesizing WAV: {ex.Message}");
            throw new InvalidOperationException($"Failed to generate WAV audio: {ex.Message}", ex);
        }
        if (audioData.Length == 0)
            throw new InvalidOperationException(strings.TtsCouldNotGenerateAudio);
        // Validate WAV header
        if (audioData.Length < 44)
        {
            System.Diagnostics.Debug.WriteLine($"[TTS] WAV data too small: {audioData.Length} bytes");
            throw new InvalidOperationException("Generated WAV data is invalid (too small)");
        }
        // Check for RIFF header
        if (audioData[0] != 'R' || audioData[1] != 'I' || audioData[2] != 'F' || audioData[3] != 'F')
        {
            System.Diagnostics.Debug.WriteLine($"[TTS] Invalid WAV header: {BitConverter.ToString(audioData, 0, 4)}");
            throw new InvalidOperationException("Generated WAV data has invalid header");
        }
        System.Diagnostics.Debug.WriteLine($"[TTS] WAV data size: {audioData.Length} bytes");
        statusCallback?.Invoke(strings.TtsConvertingToMp3);
        // Ensure target directory exists
        var targetDir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }
        // Remove existing file if present to avoid access issues
        await DeleteFileWithRetryAsync(filePath);
        // Convert WAV to MP3 using LibVLCSharp
        await ConvertWavToMp3WithLameAsync(audioData, filePath);
        statusCallback?.Invoke(string.Format(strings.TtsSavedAs, Path.GetFileName(filePath)));
    }
    /// <summary>
    /// Convert WAV data to MP3 file using LibVLCSharp
    /// </summary>
    private async Task ConvertWavToMp3WithLameAsync(byte[] wavData, string outputPath, int maxRetries = 3)
    {
        Exception? lastException = null;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                Debug.WriteLine($"[TTS] LibVLCSharp MP3 conversion attempt {attempt}/{maxRetries}");
                
                // Create temp WAV file for LibVLC input
                var tempWavPath = Path.Combine(Path.GetTempPath(), $"tts_convert_{Guid.NewGuid()}.wav");
                
                try
                {
                    // Write WAV data to temp file
                    await File.WriteAllBytesAsync(tempWavPath, wavData);
                    
                    await Task.Run(() =>
                    {
                        // Initialize LibVLC
                        Core.Initialize();
                        
                        using var libVLC = new LibVLC("--no-video");
                        
                        // Create transcoding options for MP3 output
                        var outputPathNormalized = outputPath.Replace("\\", "/");
                        var soutOption = $":sout=#transcode{{acodec=mp3,ab=192}}:std{{access=file,mux=raw,dst='{outputPathNormalized}'}}";
                        var noSoutSmemOption = ":no-sout-all";
                        var soutKeepOption = ":sout-keep";
                        
                        using var media = new Media(libVLC, tempWavPath, FromType.FromPath);
                        media.AddOption(soutOption);
                        media.AddOption(noSoutSmemOption);
                        media.AddOption(soutKeepOption);
                        
                        using var mediaPlayer = new MediaPlayer(media);
                        
                        var tcs = new TaskCompletionSource<bool>();
                        
                        mediaPlayer.EndReached += (_, _) =>
                        {
                            tcs.TrySetResult(true);
                        };
                        
                        mediaPlayer.EncounteredError += (_, _) =>
                        {
                            tcs.TrySetException(new Exception("LibVLC encountered an error during conversion"));
                        };
                        
                        mediaPlayer.Play();
                        
                        // Wait for transcoding to complete with timeout
                        if (!tcs.Task.Wait(TimeSpan.FromSeconds(60)))
                        {
                            mediaPlayer.Stop();
                            throw new TimeoutException("LibVLC transcoding timed out");
                        }
                        
                        // Stop the player to ensure file is flushed
                        mediaPlayer.Stop();
                    });
                    
                    // Small delay to ensure file is written
                    await Task.Delay(100);
                }
                finally
                {
                    // Cleanup temp WAV file
                    await DeleteFileWithRetryAsync(tempWavPath, maxRetries: 2, throwOnError: false);
                }
                
                // Verify output file
                if (!File.Exists(outputPath))
                {
                    throw new InvalidOperationException("MP3 file was not created");
                }
                var mp3Size = new FileInfo(outputPath).Length;
                Debug.WriteLine($"[TTS] MP3 saved successfully to: {outputPath}, size: {mp3Size} bytes");
                if (mp3Size == 0)
                {
                    throw new InvalidOperationException("Generated MP3 file is empty");
                }
                return; // Success!
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                lastException = ex;
                Debug.WriteLine($"[TTS] Attempt {attempt} failed: {ex.Message}");
                await Task.Delay(200 * attempt);
            }
        }
        throw new IOException($"Failed to convert WAV to MP3 after {maxRetries} attempts", lastException);
    }
    /// <summary>
    /// Delete file with retry logic for file locking issues
    /// </summary>
    private async Task DeleteFileWithRetryAsync(string filePath, int maxRetries = 3, bool throwOnError = false)
    {
        if (!File.Exists(filePath))
            return;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                File.Delete(filePath);
                return;
            }
            catch (IOException) when (attempt < maxRetries)
            {
                System.Diagnostics.Debug.WriteLine($"[TTS] Delete attempt {attempt} failed for: {filePath}");
                await Task.Delay(100 * attempt);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TTS] Could not delete file: {ex.Message}");
                if (throwOnError)
                    throw;
                return;
            }
        }
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // No unmanaged resources to dispose
    }
}
