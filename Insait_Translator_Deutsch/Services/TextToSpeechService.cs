using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
    
    // Resource markers for embedded resources
    // Resources are named like: Insait_Translator_Deutsch.PiperTTS.piper.piper.exe
    private const string PiperResourceMarker = ".PiperTTS.piper.";
    private const string ModelsResourceMarker = ".PiperTTS.models.";

    public bool IsInitialized => _isInitialized;

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

            // Extract Piper executable from embedded resources if not exists
            if (!File.Exists(AppDataPaths.PiperExePath))
            {
                statusCallback?.Invoke(strings.TtsExtractingPiper);
                await ExtractEmbeddedPiperResourcesAsync();
            }

            // Extract German voice model from embedded resources if not exists
            if (!File.Exists(AppDataPaths.ThorstenModelPath))
            {
                statusCallback?.Invoke(strings.TtsDownloadingVoiceModel);
                await ExtractEmbeddedModelResourcesAsync();
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
    /// Extract embedded Piper TTS resources to AppData
    /// </summary>
    private static async Task ExtractEmbeddedPiperResourcesAsync()
    {
        var assembly = FindAssemblyWithResources(PiperResourceMarker);
        
        if (assembly == null)
        {
            LogAvailableResources();
            throw new FileNotFoundException("PiperTTS embedded resources not found in any loaded assembly");
        }
        
        System.Diagnostics.Debug.WriteLine($"[TTS] Found PiperTTS in: {assembly.GetName().Name}");
        
        var resourceNames = assembly.GetManifestResourceNames();
        var extractedCount = 0;
        
        foreach (var resourceName in resourceNames)
        {
            if (!resourceName.Contains(PiperResourceMarker)) continue;
            
            // Get the part after the marker: e.g., "piper.exe" or "espeak_ng_data.de_dict"
            var markerIndex = resourceName.IndexOf(PiperResourceMarker, StringComparison.Ordinal);
            if (markerIndex < 0) continue;
            
            var relativePart = resourceName.Substring(markerIndex + PiperResourceMarker.Length);
            var relativePath = ConvertResourceNameToPath(relativePart);
            var outputPath = Path.Combine(AppDataPaths.PiperDirectory, relativePath);
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Extract file
            await using var resourceStream = assembly.GetManifestResourceStream(resourceName);
            if (resourceStream != null)
            {
                await using var fileStream = File.Create(outputPath);
                await resourceStream.CopyToAsync(fileStream);
                extractedCount++;
            }
        }
        
        System.Diagnostics.Debug.WriteLine($"[TTS] Extracted {extractedCount} Piper files");
        
        if (extractedCount == 0)
        {
            throw new FileNotFoundException("No PiperTTS resources found to extract");
        }
    }

    /// <summary>
    /// Extract embedded voice model resources to AppData
    /// </summary>
    private static async Task ExtractEmbeddedModelResourcesAsync()
    {
        var assembly = FindAssemblyWithResources(ModelsResourceMarker);
        
        if (assembly == null)
        {
            throw new FileNotFoundException("Model embedded resources not found in any loaded assembly");
        }
        
        System.Diagnostics.Debug.WriteLine($"[TTS] Found models in: {assembly.GetName().Name}");
        
        var resourceNames = assembly.GetManifestResourceNames();
        var extractedCount = 0;
        
        foreach (var resourceName in resourceNames)
        {
            if (!resourceName.Contains(ModelsResourceMarker)) continue;
            
            var markerIndex = resourceName.IndexOf(ModelsResourceMarker, StringComparison.Ordinal);
            if (markerIndex < 0) continue;
            
            // Get filename: e.g., "de_DE_thorsten_high.onnx"
            var fileName = resourceName.Substring(markerIndex + ModelsResourceMarker.Length);
            
            // Fix filename: convert underscores to dashes (except locale part)
            // de_DE_thorsten_high.onnx -> de_DE-thorsten-high.onnx
            fileName = FixModelFileName(fileName);
            
            var outputPath = Path.Combine(AppDataPaths.ModelsDirectory, fileName);
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Extract file
            await using var resourceStream = assembly.GetManifestResourceStream(resourceName);
            if (resourceStream != null)
            {
                await using var fileStream = File.Create(outputPath);
                await resourceStream.CopyToAsync(fileStream);
                extractedCount++;
            }
        }
        
        System.Diagnostics.Debug.WriteLine($"[TTS] Extracted {extractedCount} model files");
        
        if (extractedCount == 0)
        {
            throw new FileNotFoundException("No model resources found to extract");
        }
    }
    
    /// <summary>
    /// Find assembly containing resources with specified marker
    /// </summary>
    private static Assembly? FindAssemblyWithResources(string marker)
    {
        // First try this library assembly
        var currentAssembly = typeof(TextToSpeechService).Assembly;
        if (HasResourcesWithMarker(currentAssembly, marker))
            return currentAssembly;
        
        // Try entry assembly
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != null && HasResourcesWithMarker(entryAssembly, marker))
            return entryAssembly;
        
        // Search all loaded assemblies
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                if (HasResourcesWithMarker(assembly, marker))
                    return assembly;
            }
            catch { /* Skip */ }
        }
        
        return null;
    }
    
    private static bool HasResourcesWithMarker(Assembly assembly, string marker)
    {
        try
        {
            return assembly.GetManifestResourceNames().Any(r => r.Contains(marker));
        }
        catch { return false; }
    }
    
    private static void LogAvailableResources()
    {
        System.Diagnostics.Debug.WriteLine("[TTS] ERROR: No PiperTTS resources found!");
        System.Diagnostics.Debug.WriteLine("[TTS] Checking loaded assemblies:");
        
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var names = asm.GetManifestResourceNames();
                var piperNames = names.Where(n => n.Contains("Piper", StringComparison.OrdinalIgnoreCase)).ToList();
                if (piperNames.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"  {asm.GetName().Name}: {piperNames.Count} Piper resources");
                    foreach (var name in piperNames.Take(5))
                        System.Diagnostics.Debug.WriteLine($"    - {name}");
                }
            }
            catch { /* Skip */ }
        }
    }
    
    /// <summary>
    /// Convert resource name to file path
    /// Example: "piper.exe" -> "piper.exe"
    /// Example: "espeak_ng_data.de_dict" -> "espeak-ng-data\de_dict"
    /// </summary>
    private static string ConvertResourceNameToPath(string resourcePart)
    {
        // Known file extensions
        string[] extensions = { ".exe", ".dll", ".onnx", ".json", ".ort" };
        
        // Check for .onnx.json first (compound extension)
        if (resourcePart.EndsWith(".onnx.json", StringComparison.OrdinalIgnoreCase))
        {
            var pathPart = resourcePart[..^".onnx.json".Length];
            return ConvertDotsToPath(pathPart) + ".onnx.json";
        }
        
        // Check known extensions
        foreach (var ext in extensions)
        {
            if (resourcePart.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            {
                var pathPart = resourcePart[..^ext.Length];
                return ConvertDotsToPath(pathPart) + ext;
            }
        }
        
        // No known extension - treat as directory/file without extension
        return ConvertDotsToPath(resourcePart);
    }
    
    /// <summary>
    /// Convert dots to path separators, handling special cases like espeak-ng-data
    /// </summary>
    private static string ConvertDotsToPath(string part)
    {
        // Replace underscores that should be dashes (espeak_ng_data -> espeak-ng-data)
        // But keep locale underscores (de_dict stays de_dict)
        
        // First, handle known folder names
        part = part.Replace("espeak_ng_data", "espeak-ng-data");
        
        // Now replace dots with path separators
        return part.Replace('.', Path.DirectorySeparatorChar);
    }
    
    /// <summary>
    /// Fix model filename: convert underscores to dashes except locale
    /// de_DE_thorsten_high.onnx -> de_DE-thorsten-high.onnx
    /// </summary>
    private static string FixModelFileName(string fileName)
    {
        // Find extension
        var extIndex = fileName.IndexOf(".onnx", StringComparison.OrdinalIgnoreCase);
        if (extIndex < 0) return fileName;
        
        var namePart = fileName[..extIndex];
        var extPart = fileName[extIndex..];
        
        // Split by underscore
        var parts = namePart.Split('_');
        if (parts.Length >= 3)
        {
            // Keep first two with underscore (de_DE), join rest with dashes
            return $"{parts[0]}_{parts[1]}-{string.Join("-", parts.Skip(2))}{extPart}";
        }
        
        return fileName;
    }

    /// <summary>
    /// Synthesize text to WAV audio bytes
    /// </summary>
    public async Task<byte[]> SynthesizeToWavAsync(string text)
    {
        var strings = LocalizationManager.Instance.Strings;
        
        if (!_isInitialized)
            throw new InvalidOperationException(strings.TtsNotInitialized);

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

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            await process.StandardInput.WriteLineAsync(text);
            process.StandardInput.Close();

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

            if (!File.Exists(outputPath))
                throw new FileNotFoundException(strings.TtsAudioFileNotCreated);
            
            return await File.ReadAllBytesAsync(outputPath);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                try { File.Delete(outputPath); } catch { /* ignore */ }
            }
        }
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

        var audioData = await SynthesizeToWavAsync(text);
        
        if (audioData.Length == 0)
            throw new InvalidOperationException(strings.TtsCouldNotGenerateAudio);

        statusCallback?.Invoke(strings.TtsConvertingToMp3);

        using var wavStream = new MemoryStream(audioData);
        using var reader = new WaveFileReader(wavStream);
        
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
