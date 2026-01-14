using System;
using System.IO;
using LiteDB;
using Phoenix.Core.Models;

namespace Phoenix.Data;

/// <summary>
/// LiteDB database context for the Phoenix ePub Reader.
/// </summary>
public class PhoenixDatabase : IDisposable
{
    private readonly LiteDatabase _database;
    private bool _disposed;

    /// <summary>
    /// Gets the Books collection.
    /// </summary>
    public ILiteCollection<Book> Books => _database.GetCollection<Book>("books");

    /// <summary>
    /// Gets the Bookmarks collection.
    /// </summary>
    public ILiteCollection<Bookmark> Bookmarks => _database.GetCollection<Bookmark>("bookmarks");

    /// <summary>
    /// Gets the ReadingProgress collection.
    /// </summary>
    public ILiteCollection<ReadingProgress> ReadingProgress => _database.GetCollection<ReadingProgress>("reading_progress");

    /// <summary>
    /// Gets the UserSettings collection.
    /// </summary>
    public ILiteCollection<UserSettings> Settings => _database.GetCollection<UserSettings>("settings");

    /// <summary>
    /// Initializes a new instance of the PhoenixDatabase.
    /// </summary>
    /// <param name="databasePath">The path to the database file. If null, uses the default location.</param>
    public PhoenixDatabase(string? databasePath = null)
    {
        var dbPath = databasePath ?? GetDefaultDatabasePath();
        
        // Ensure directory exists
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _database = new LiteDatabase(dbPath);
        
        // Create indexes
        EnsureIndexes();
    }

    private static string GetDefaultDatabasePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var phoenixFolder = Path.Combine(appData, "PhoenixEpubReader");
        return Path.Combine(phoenixFolder, "phoenix.db");
    }

    private void EnsureIndexes()
    {
        // Book indexes
        Books.EnsureIndex(b => b.Id, unique: true);
        Books.EnsureIndex(b => b.FilePath);
        Books.EnsureIndex(b => b.Title);
        Books.EnsureIndex(b => b.Author);
        Books.EnsureIndex(b => b.LastOpened);

        // Bookmark indexes
        Bookmarks.EnsureIndex(bm => bm.Id, unique: true);
        Bookmarks.EnsureIndex(bm => bm.BookId);

        // Reading progress indexes
        ReadingProgress.EnsureIndex(rp => rp.Id, unique: true);
        ReadingProgress.EnsureIndex(rp => rp.BookId, unique: true);

        // Settings index
        Settings.EnsureIndex(s => s.Id, unique: true);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _database?.Dispose();
            }
            _disposed = true;
        }
    }
}
