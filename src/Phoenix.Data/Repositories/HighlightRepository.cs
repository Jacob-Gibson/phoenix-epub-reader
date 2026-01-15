using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Phoenix.Core.Interfaces;
using Phoenix.Core.Models;

namespace Phoenix.Data.Repositories;

/// <summary>
/// LiteDB implementation of the highlight service.
/// </summary>
public class HighlightRepository : IHighlightService
{
    private readonly PhoenixDatabase _database;

    public HighlightRepository(PhoenixDatabase database)
    {
        _database = database;
    }

    /// <inheritdoc />
    public Task<IEnumerable<Highlight>> GetHighlightsAsync(Guid bookId)
    {
        var highlights = _database.Highlights
            .Find(h => h.BookId == bookId)
            .OrderBy(h => h.CreatedAt)
            .ToList();
        
        return Task.FromResult<IEnumerable<Highlight>>(highlights);
    }

    /// <inheritdoc />
    public Task<IEnumerable<Highlight>> GetHighlightsForChapterAsync(Guid bookId, string contentPath)
    {
        var highlights = _database.Highlights
            .Find(h => h.BookId == bookId && h.ContentPath == contentPath)
            .OrderBy(h => h.CreatedAt)
            .ToList();
        
        return Task.FromResult<IEnumerable<Highlight>>(highlights);
    }

    /// <inheritdoc />
    public Task<Highlight?> GetHighlightByIdAsync(Guid id)
    {
        var highlight = _database.Highlights.FindOne(h => h.Id == id);
        return Task.FromResult<Highlight?>(highlight);
    }

    /// <inheritdoc />
    public Task<Highlight> AddHighlightAsync(Highlight highlight)
    {
        _database.Highlights.Insert(highlight);
        return Task.FromResult(highlight);
    }

    /// <inheritdoc />
    public Task<bool> UpdateHighlightAsync(Highlight highlight)
    {
        highlight.ModifiedAt = DateTime.UtcNow;
        var result = _database.Highlights.Update(highlight);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<bool> DeleteHighlightAsync(Guid id)
    {
        var result = _database.Highlights.Delete(id);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAllHighlightsAsync(Guid bookId)
    {
        var count = _database.Highlights.DeleteMany(h => h.BookId == bookId);
        return Task.FromResult(count > 0);
    }
}
