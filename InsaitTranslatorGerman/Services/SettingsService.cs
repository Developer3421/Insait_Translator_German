using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using LiteDB;

namespace InsaitTranslatorGerman.Services;

/// <summary>
/// Secure settings storage using LiteDB with AES encryption.
/// Encryption key is derived from Windows DPAPI (user-level protection).
/// Database is automatically recreated if corrupted.
/// Default interface language is English if database is corrupted or doesn't exist.
/// </summary>
public class SettingsService : IDisposable
{
    private static SettingsService? _instance;
    public static SettingsService Instance => _instance ??= new SettingsService();

    private LiteDatabase? _db;
    private ILiteCollection<SettingEntry>? _settings;
    private readonly byte[] _encryptionKey;
    private readonly string _dbPath;
    private bool _databaseCorrupted;

    private const string SettingsCollection = "settings";
    private const string DefaultLanguage = "en"; // English as default if DB corrupted or missing

    public SettingsService()
    {
        // Generate/retrieve encryption key using Windows DPAPI
        _encryptionKey = GetOrCreateEncryptionKey();

        // Initialize LiteDB - using AppDataPaths for Microsoft Store compatibility
        _dbPath = AppDataPaths.SettingsDatabasePath;

        InitializeDatabase();
    }

    /// <summary>
    /// Safely initializes the database, recreating it if corrupted.
    /// </summary>
    private void InitializeDatabase()
    {
        _databaseCorrupted = false;
        
        try
        {
            _db = new LiteDatabase($"Filename={_dbPath};Connection=shared");
            _settings = _db.GetCollection<SettingEntry>(SettingsCollection);
            _settings.EnsureIndex(x => x.Key, true);
            
            // Test database integrity by performing a simple read
            _ = _settings.Count();
        }
        catch (LiteException)
        {
            HandleCorruptedDatabase();
        }
        catch (IOException)
        {
            HandleCorruptedDatabase();
        }
        catch (Exception)
        {
            HandleCorruptedDatabase();
        }
    }

    /// <summary>
    /// Handles corrupted database by deleting and recreating it.
    /// </summary>
    private void HandleCorruptedDatabase()
    {
        _databaseCorrupted = true;
        
        try
        {
            _db?.Dispose();
            _db = null;
        }
        catch { /* ignore dispose errors */ }

        try
        {
            // Delete corrupted database files
            if (File.Exists(_dbPath))
                File.Delete(_dbPath);
            
            var logPath = _dbPath + "-log";
            if (File.Exists(logPath))
                File.Delete(logPath);
        }
        catch { /* ignore deletion errors */ }

        try
        {
            // Create fresh database
            _db = new LiteDatabase($"Filename={_dbPath};Connection=shared");
            _settings = _db.GetCollection<SettingEntry>(SettingsCollection);
            _settings.EnsureIndex(x => x.Key, true);
        }
        catch
        {
            // If we still can't create the database, work in memory-only mode
            _db = null;
            _settings = null;
        }
    }

    /// <summary>
    /// Indicates if the database was corrupted and recreated.
    /// </summary>
    public bool WasDatabaseCorrupted => _databaseCorrupted;

    /// <summary>
    /// Gets or creates an encryption key protected by Windows DPAPI.
    /// The key is unique per Windows user and cannot be decrypted by other users.
    /// </summary>
    private byte[] GetOrCreateEncryptionKey()
    {
        var keyPath = AppDataPaths.EncryptionKeyPath;

        // Ensure base directory exists
        var keyDir = Path.GetDirectoryName(keyPath);
        if (!string.IsNullOrEmpty(keyDir) && !Directory.Exists(keyDir))
        {
            Directory.CreateDirectory(keyDir);
        }

        if (File.Exists(keyPath))
        {
            try
            {
                var protectedKey = File.ReadAllBytes(keyPath);
                return ProtectedData.Unprotect(protectedKey, null, DataProtectionScope.CurrentUser);
            }
            catch
            {
                // Key file corrupted, regenerate - remove hidden attribute first
                try
                {
                    File.SetAttributes(keyPath, FileAttributes.Normal);
                    File.Delete(keyPath);
                }
                catch { /* ignore deletion errors */ }
            }
        }

        // Generate new 256-bit key
        var key = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(key);
        }

        // Protect with DPAPI and save
        byte[] protectedBytes = ProtectedData.Protect(key, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(keyPath, protectedBytes);

        // Hide the key file
        try
        {
            File.SetAttributes(keyPath, FileAttributes.Hidden);
        }
        catch { /* ignore attribute errors */ }

        return key;
    }

    /// <summary>
    /// Encrypts a string value using AES-256.
    /// </summary>
    private string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Combine IV + encrypted data
        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

        return Convert.ToBase64String(result);
    }

    /// <summary>
    /// Decrypts an AES-256 encrypted string.
    /// </summary>
    private string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return string.Empty;

        try
        {
            var fullCipher = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = _encryptionKey;

            // Extract IV (first 16 bytes)
            var iv = new byte[16];
            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            aes.IV = iv;

            // Extract encrypted data
            var cipher = new byte[fullCipher.Length - iv.Length];
            Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Stores a setting with automatic AES encryption for sensitive values.
    /// </summary>
    public void SetSetting(string key, string value, bool encrypt = false)
    {
        if (_settings == null) return; // Database unavailable, skip

        try
        {
            var storedValue = encrypt ? Encrypt(value) : value;

            var existing = _settings.FindOne(x => x.Key == key);
            if (existing != null)
            {
                existing.Value = storedValue;
                existing.IsEncrypted = encrypt;
                _settings.Update(existing);
            }
            else
            {
                _settings.Insert(new SettingEntry
                {
                    Key = key,
                    Value = storedValue,
                    IsEncrypted = encrypt
                });
            }
        }
        catch (LiteException)
        {
            HandleCorruptedDatabase();
        }
        catch (IOException)
        {
            HandleCorruptedDatabase();
        }
    }

    /// <summary>
    /// Retrieves a setting with automatic decryption if needed.
    /// </summary>
    public string GetSetting(string key, string defaultValue = "")
    {
        if (_settings == null) return defaultValue; // Database unavailable

        try
        {
            var entry = _settings.FindOne(x => x.Key == key);
            if (entry == null)
                return defaultValue;

            return entry.IsEncrypted ? Decrypt(entry.Value) : entry.Value;
        }
        catch (LiteException)
        {
            HandleCorruptedDatabase();
            return defaultValue;
        }
        catch (IOException)
        {
            HandleCorruptedDatabase();
            return defaultValue;
        }
    }

    /// <summary>
    /// Checks if a setting exists.
    /// </summary>
    public bool HasSetting(string key)
    {
        if (_settings == null) return false; // Database unavailable

        try
        {
            return _settings.Exists(x => x.Key == key);
        }
        catch (LiteException)
        {
            HandleCorruptedDatabase();
            return false;
        }
        catch (IOException)
        {
            HandleCorruptedDatabase();
            return false;
        }
    }

    /// <summary>
    /// Removes a setting.
    /// </summary>
    public void RemoveSetting(string key)
    {
        if (_settings == null) return; // Database unavailable

        try
        {
            _settings.DeleteMany(x => x.Key == key);
        }
        catch (LiteException)
        {
            HandleCorruptedDatabase();
        }
        catch (IOException)
        {
            HandleCorruptedDatabase();
        }
    }

    // Convenience properties for common settings
    public string GoogleApiKey
    {
        get => GetSetting("GoogleApiKey");
        set => SetSetting("GoogleApiKey", value, encrypt: true);
    }

    public bool UseGoogleApi
    {
        get => GetSetting("UseGoogleApi", "false") == "true";
        set => SetSetting("UseGoogleApi", value ? "true" : "false");
    }

    public string PreferredTranslationProvider
    {
        get => GetSetting("PreferredTranslationProvider", "auto");
        set => SetSetting("PreferredTranslationProvider", value);
    }

    /// <summary>
    /// Interface language code (uk, en, de)
    /// Default is English if database is corrupted or unavailable.
    /// </summary>
    public string InterfaceLanguage
    {
        get => _databaseCorrupted ? DefaultLanguage : GetSetting("InterfaceLanguage", DefaultLanguage);
        set => SetSetting("InterfaceLanguage", value);
    }

    public void Dispose()
    {
        _db?.Dispose();
    }
}

/// <summary>
/// Database entity for storing settings.
/// </summary>
public class SettingEntry
{
    public ObjectId Id { get; set; } = ObjectId.NewObjectId();
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsEncrypted { get; set; }
}
