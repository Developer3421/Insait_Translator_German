using System;
using System.IO;
using System.Reflection;

namespace InsaitTranslatorGerman.Services;

/// <summary>
/// Centralized application data paths management for Microsoft Store compatibility.
/// All application data (AI models, TTS, database) is stored in LocalApplicationData.
/// </summary>
public static class AppDataPaths
{
    private static readonly Lazy<string> _basePath = new(() =>
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "InsaitTranslatorGerman");
        
        EnsureDirectoryExists(path);
        return path;
    });

    private static readonly Lazy<string> _appDirectory = new(() =>
    {
        // Try multiple methods to get the application directory for maximum compatibility
        
        // Method 1: Process path (most reliable for published apps)
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(processPath))
        {
            var processDir = Path.GetDirectoryName(processPath);
            if (!string.IsNullOrEmpty(processDir) && Directory.Exists(processDir))
            {
                return processDir;
            }
        }
        
        // Method 2: BaseDirectory (works for most scenarios)
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        if (!string.IsNullOrEmpty(baseDir) && Directory.Exists(baseDir))
        {
            return baseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        
        // Method 3: Executing assembly location (may be empty for single-file apps)
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyLocation = assembly.Location;
        if (!string.IsNullOrEmpty(assemblyLocation))
        {
            var assemblyDir = Path.GetDirectoryName(assemblyLocation);
            if (!string.IsNullOrEmpty(assemblyDir) && Directory.Exists(assemblyDir))
            {
                return assemblyDir;
            }
        }
        
        // Method 4: Entry assembly
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != null && !string.IsNullOrEmpty(entryAssembly.Location))
        {
            var entryDir = Path.GetDirectoryName(entryAssembly.Location);
            if (!string.IsNullOrEmpty(entryDir) && Directory.Exists(entryDir))
            {
                return entryDir;
            }
        }
        
        // Fallback: current directory
        return Environment.CurrentDirectory;
    });

    /// <summary>
    /// Application installation directory (where the .exe is located)
    /// </summary>
    public static string AppDirectory => _appDirectory.Value;

    /// <summary>
    /// Base application data directory: %LOCALAPPDATA%\InsaitTranslatorGerman
    /// </summary>
    public static string BasePath => _basePath.Value;

    /// <summary>
    /// Directory for Piper TTS executable and dependencies in the application directory
    /// Files are bundled with the application in PiperTTS\piper folder
    /// </summary>
    public static string BundledPiperDirectory => Path.Combine(AppDirectory, "PiperTTS", "piper");

    /// <summary>
    /// Path to bundled Piper executable in the application directory
    /// </summary>
    public static string BundledPiperExePath => Path.Combine(BundledPiperDirectory, "piper.exe");

    /// <summary>
    /// Directory for bundled voice models in the application directory
    /// </summary>
    public static string BundledModelsDirectory => Path.Combine(AppDirectory, "PiperTTS", "models");

    /// <summary>
    /// Path to bundled German Thorsten voice model
    /// </summary>
    public static string BundledThorstenModelPath => Path.Combine(BundledModelsDirectory, "de_DE-thorsten-high.onnx");

    /// <summary>
    /// Path to bundled German Thorsten voice model configuration
    /// </summary>
    public static string BundledThorstenModelConfigPath => Path.Combine(BundledModelsDirectory, "de_DE-thorsten-high.onnx.json");

    /// <summary>
    /// Directory for Piper TTS executable and dependencies
    /// Fallback to AppData if bundled version is not found
    /// </summary>
    public static string PiperDirectory
    {
        get
        {
            // First check if bundled Piper exists
            if (Directory.Exists(BundledPiperDirectory) && File.Exists(BundledPiperExePath))
            {
                return BundledPiperDirectory;
            }
            
            // Fallback to AppData
            var path = Path.Combine(BasePath, "piper");
            EnsureDirectoryExists(path);
            return path;
        }
    }

    /// <summary>
    /// Path to Piper executable
    /// Uses bundled version if available, otherwise falls back to AppData
    /// </summary>
    public static string PiperExePath
    {
        get
        {
            // First check bundled version
            if (File.Exists(BundledPiperExePath))
            {
                return BundledPiperExePath;
            }
            
            // Fallback to AppData
            return Path.Combine(BasePath, "piper", "piper.exe");
        }
    }

    /// <summary>
    /// Directory for AI voice models (Thorsten and others)
    /// Uses bundled version if available
    /// </summary>
    public static string ModelsDirectory
    {
        get
        {
            // First check bundled version
            if (Directory.Exists(BundledModelsDirectory) && File.Exists(BundledThorstenModelPath))
            {
                return BundledModelsDirectory;
            }
            
            // Fallback to AppData
            var path = Path.Combine(BasePath, "models");
            EnsureDirectoryExists(path);
            return path;
        }
    }

    /// <summary>
    /// Path to German Thorsten voice model
    /// Uses bundled version if available
    /// </summary>
    public static string ThorstenModelPath
    {
        get
        {
            // First check bundled version
            if (File.Exists(BundledThorstenModelPath))
            {
                return BundledThorstenModelPath;
            }
            
            // Fallback to AppData
            return Path.Combine(BasePath, "models", "de_DE-thorsten-high.onnx");
        }
    }

    /// <summary>
    /// Path to German Thorsten voice model configuration
    /// Uses bundled version if available
    /// </summary>
    public static string ThorstenModelConfigPath
    {
        get
        {
            // First check bundled version
            if (File.Exists(BundledThorstenModelConfigPath))
            {
                return BundledThorstenModelConfigPath;
            }
            
            // Fallback to AppData
            return Path.Combine(BasePath, "models", "de_DE-thorsten-high.onnx.json");
        }
    }

    /// <summary>
    /// Check if Piper TTS is bundled with the application
    /// </summary>
    public static bool IsPiperBundled => File.Exists(BundledPiperExePath) && File.Exists(BundledThorstenModelPath);

    /// <summary>
    /// Directory for LiteDB database files
    /// </summary>
    public static string DatabaseDirectory
    {
        get
        {
            var path = Path.Combine(BasePath, "data");
            EnsureDirectoryExists(path);
            return path;
        }
    }

    /// <summary>
    /// Path to settings database file
    /// </summary>
    public static string SettingsDatabasePath => Path.Combine(BasePath, "insait_translator_settings.db");

    /// <summary>
    /// Path to workspace database file (tab content with 100MB limit)
    /// </summary>
    public static string WorkspaceDatabasePath => Path.Combine(BasePath, "insait_translator_workspaces.db");

    /// <summary>
    /// Path to encryption key file
    /// </summary>
    public static string EncryptionKeyPath => Path.Combine(BasePath, ".enckey");

    /// <summary>
    /// Directory for temporary files (audio processing, etc.)
    /// Used for:
    /// - Temporary WAV files during TTS synthesis
    /// - Intermediate audio files during MP3 conversion
    /// - Files are automatically cleaned up after processing
    /// - Old files (>24 hours) are removed by CleanupTempFiles()
    /// </summary>
    public static string TempDirectory
    {
        get
        {
            var path = Path.Combine(BasePath, "temp");
            EnsureDirectoryExists(path);
            return path;
        }
    }

    /// <summary>
    /// Directory for cache files
    /// </summary>
    public static string CacheDirectory
    {
        get
        {
            var path = Path.Combine(BasePath, "cache");
            EnsureDirectoryExists(path);
            return path;
        }
    }

    /// <summary>
    /// Directory for log files
    /// </summary>
    public static string LogsDirectory
    {
        get
        {
            var path = Path.Combine(BasePath, "logs");
            EnsureDirectoryExists(path);
            return path;
        }
    }

    /// <summary>
    /// Generates a unique temporary file path with specified extension
    /// </summary>
    public static string GetTempFilePath(string extension = ".tmp")
    {
        if (!extension.StartsWith('.'))
            extension = "." + extension;
        
        return Path.Combine(TempDirectory, $"temp_{Guid.NewGuid()}{extension}");
    }

    /// <summary>
    /// Cleans up temporary files older than specified age
    /// </summary>
    public static void CleanupTempFiles(TimeSpan? maxAge = null)
    {
        maxAge ??= TimeSpan.FromHours(24);
        
        try
        {
            if (!Directory.Exists(TempDirectory))
                return;

            var cutoff = DateTime.UtcNow - maxAge.Value;
            foreach (var file in Directory.GetFiles(TempDirectory))
            {
                try
                {
                    if (File.GetLastWriteTimeUtc(file) < cutoff)
                    {
                        File.Delete(file);
                    }
                }
                catch
                {
                    // Ignore individual file deletion errors
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}

