using System;
using System.IO;
using LiteDB;

namespace Insait_Translator_Deutsch.Services;

/// <summary>
/// Separate LiteDB database for workspace tab content with size limits.
/// Database is automatically recreated if corrupted.
/// Maximum database size is limited to 100 MB.
/// </summary>
public class WorkspaceDatabaseService : IDisposable
{
    private static WorkspaceDatabaseService? _instance;
    public static WorkspaceDatabaseService Instance => _instance ??= new WorkspaceDatabaseService();

    private LiteDatabase? _db;
    private ILiteCollection<WorkspaceEntry>? _workspaces;
    private readonly string _dbPath;
    private bool _databaseCorrupted;

    private const string WorkspacesCollection = "workspaces";
    private const long MaxDatabaseSizeBytes = 100 * 1024 * 1024; // 100 MB

    public WorkspaceDatabaseService()
    {
        _dbPath = AppDataPaths.WorkspaceDatabasePath;
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
            _workspaces = _db.GetCollection<WorkspaceEntry>(WorkspacesCollection);
            _workspaces.EnsureIndex(x => x.Key, true);

            // Test database integrity by performing a simple read
            _ = _workspaces.Count();
            
            // Check database size and shrink if needed
            CheckAndEnforceSizeLimit();
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
            _workspaces = _db.GetCollection<WorkspaceEntry>(WorkspacesCollection);
            _workspaces.EnsureIndex(x => x.Key, true);
        }
        catch
        {
            // If we still can't create the database, work in memory-only mode
            _db = null;
            _workspaces = null;
        }
    }

    /// <summary>
    /// Checks database size and shrinks/cleans if exceeds limit.
    /// </summary>
    private void CheckAndEnforceSizeLimit()
    {
        try
        {
            if (!File.Exists(_dbPath)) return;

            var fileInfo = new FileInfo(_dbPath);
            if (fileInfo.Length <= MaxDatabaseSizeBytes) return;

            // Database exceeds size limit, try to shrink
            _db?.Checkpoint();
            _db?.Rebuild();

            // Check again after rebuild
            fileInfo.Refresh();
            if (fileInfo.Length > MaxDatabaseSizeBytes)
            {
                // If still too large, clear old data and rebuild
                _workspaces?.DeleteAll();
                _db?.Checkpoint();
                _db?.Rebuild();
            }
        }
        catch
        {
            // Ignore size check errors
        }
    }

    /// <summary>
    /// Gets the current database size in bytes.
    /// </summary>
    public long GetDatabaseSizeBytes()
    {
        try
        {
            if (File.Exists(_dbPath))
            {
                var fileInfo = new FileInfo(_dbPath);
                return fileInfo.Length;
            }
        }
        catch { }
        return 0;
    }

    /// <summary>
    /// Indicates if the database was corrupted and recreated.
    /// </summary>
    public bool WasDatabaseCorrupted => _databaseCorrupted;

    /// <summary>
    /// Checks if there's enough space for a new entry (estimated).
    /// </summary>
    public bool HasSpaceForEntry(int estimatedBytes = 10000)
    {
        return GetDatabaseSizeBytes() + estimatedBytes <= MaxDatabaseSizeBytes;
    }

    /// <summary>
    /// Stores a workspace entry.
    /// </summary>
    public void SetEntry(string key, string value)
    {
        if (_workspaces == null) return;

        try
        {
            // Check size limit before adding
            var estimatedSize = System.Text.Encoding.UTF8.GetByteCount(value);
            if (!HasSpaceForEntry(estimatedSize))
            {
                // Try to make space by shrinking
                CheckAndEnforceSizeLimit();
            }

            var existing = _workspaces.FindOne(x => x.Key == key);
            if (existing != null)
            {
                existing.Value = value;
                existing.UpdatedAt = DateTime.UtcNow;
                _workspaces.Update(existing);
            }
            else
            {
                _workspaces.Insert(new WorkspaceEntry
                {
                    Key = key,
                    Value = value,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
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
    /// Retrieves a workspace entry.
    /// </summary>
    public string GetEntry(string key, string defaultValue = "")
    {
        if (_workspaces == null) return defaultValue;

        try
        {
            var entry = _workspaces.FindOne(x => x.Key == key);
            return entry?.Value ?? defaultValue;
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
    /// Removes a workspace entry.
    /// </summary>
    public void RemoveEntry(string key)
    {
        if (_workspaces == null) return;

        try
        {
            _workspaces.DeleteMany(x => x.Key == key);
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
    /// Compacts the database to reclaim space.
    /// </summary>
    public void Compact()
    {
        try
        {
            _db?.Checkpoint();
            _db?.Rebuild();
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

    public void Dispose()
    {
        _db?.Dispose();
    }
}

/// <summary>
/// Database entity for storing workspace data.
/// </summary>
public class WorkspaceEntry
{
    public ObjectId Id { get; set; } = ObjectId.NewObjectId();
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

