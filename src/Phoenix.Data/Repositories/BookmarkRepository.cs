using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Phoenix.Core.Interfaces;
using Phoenix.Core.Models;

namespace Phoenix.Data.Repositories;

/// <summary>
/// LiteDB implementation of the bookmark service.
/// </summary>
public class BookmarkRepository : IBookmarkService
{
    private readonly PhoenixDatabase _database;

    public BookmarkRepository(PhoenixDatabase database)
    {
        _database = database;
    }

    /// <inheritdoc />
    public Task<IEnumerable<Bookmark>> GetBookmarksAsync(Guid bookId)
    {
        var bookmarks = _database.Bookmarks
            .Find(bm => bm.BookId == bookId)
            .OrderBy(bm => bm.CreatedAt)
            .ToList();
        
        return Task.FromResult<IEnumerable<Bookmark>>(bookmarks);
    }

    /// <inheritdoc />
    public Task<Bookmark?> GetBookmarkByIdAsync(Guid id)
    {
        var bookmark = _database.Bookmarks.FindOne(bm => bm.Id == id);
        return Task.FromResult<Bookmark?>(bookmark);
    }

    /// <inheritdoc />
    public Task<Bookmark> AddBookmarkAsync(Bookmark bookmark)
    {
        _database.Bookmarks.Insert(bookmark);
        return Task.FromResult(bookmark);
    }

    /// <inheritdoc />
    public Task<bool> UpdateBookmarkAsync(Bookmark bookmark)
    {
        var result = _database.Bookmarks.Update(bookmark);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<bool> DeleteBookmarkAsync(Guid id)
    {
        var result = _database.Bookmarks.Delete(id);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAllBookmarksAsync(Guid bookId)
    {
        var count = _database.Bookmarks.DeleteMany(bm => bm.BookId == bookId);
        return Task.FromResult(count > 0);
    }
}
