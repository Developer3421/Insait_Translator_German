using System;
using System.IO;

namespace Insait_Translator_Deutsch.Services;

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
            "InsaitTranslator");
        
        EnsureDirectoryExists(path);
        return path;
    });

    /// <summary>
    /// Base application data directory: %LOCALAPPDATA%\InsaitTranslator
    /// </summary>
    public static string BasePath => _basePath.Value;

    /// <summary>
    /// Directory for Piper TTS executable and dependencies
    /// </summary>
    public static string PiperDirectory
    {
        get
        {
            var path = Path.Combine(BasePath, "piper");
            EnsureDirectoryExists(path);
            return path;
        }
    }

    /// <summary>
    /// Path to Piper executable
    /// </summary>
    public static string PiperExePath => Path.Combine(PiperDirectory, "piper", "piper.exe");

    /// <summary>
    /// Directory for AI voice models (Thorsten and others)
    /// </summary>
    public static string ModelsDirectory
    {
        get
        {
            var path = Path.Combine(BasePath, "models");
            EnsureDirectoryExists(path);
            return path;
        }
    }

    /// <summary>
    /// Path to German Thorsten voice model
    /// </summary>
    public static string ThorstenModelPath => Path.Combine(ModelsDirectory, "de_DE-thorsten-high.onnx");

    /// <summary>
    /// Path to German Thorsten voice model configuration
    /// </summary>
    public static string ThorstenModelConfigPath => Path.Combine(ModelsDirectory, "de_DE-thorsten-high.onnx.json");

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

