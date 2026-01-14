using System;
using System.Linq;
using System.Threading.Tasks;
using Phoenix.Core.Interfaces;
using Phoenix.Core.Models;

namespace Phoenix.Data.Repositories;

/// <summary>
/// LiteDB implementation of the reading progress service.
/// </summary>
public class ReadingProgressRepository : IReadingProgressService
{
    private readonly PhoenixDatabase _database;

    public ReadingProgressRepository(PhoenixDatabase database)
    {
        _database = database;
    }

    /// <inheritdoc />
    public Task<ReadingProgress?> GetProgressAsync(Guid bookId)
    {
        var progress = _database.ReadingProgress.FindOne(rp => rp.BookId == bookId);
        return Task.FromResult<ReadingProgress?>(progress);
    }

    /// <inheritdoc />
    public Task<ReadingProgress> SaveProgressAsync(ReadingProgress progress)
    {
        progress.LastUpdated = DateTime.UtcNow;
        
        var existing = _database.ReadingProgress.FindOne(rp => rp.BookId == progress.BookId);
        
        if (existing != null)
        {
            progress.Id = existing.Id;
            _database.ReadingProgress.Update(progress);
        }
        else
        {
            _database.ReadingProgress.Insert(progress);
        }
        
        return Task.FromResult(progress);
    }

    /// <inheritdoc />
    public Task<bool> DeleteProgressAsync(Guid bookId)
    {
        var count = _database.ReadingProgress.DeleteMany(rp => rp.BookId == bookId);
        return Task.FromResult(count > 0);
    }
}
