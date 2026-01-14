using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Phoenix.Core.Models;

namespace Phoenix.Core.Interfaces;

/// <summary>
/// Interface for managing bookmarks.
/// </summary>
public interface IBookmarkService
{
    /// <summary>
    /// Gets all bookmarks for a book.
    /// </summary>
    Task<IEnumerable<Bookmark>> GetBookmarksAsync(Guid bookId);

    /// <summary>
    /// Gets a bookmark by ID.
    /// </summary>
    Task<Bookmark?> GetBookmarkByIdAsync(Guid id);

    /// <summary>
    /// Adds a new bookmark.
    /// </summary>
    Task<Bookmark> AddBookmarkAsync(Bookmark bookmark);

    /// <summary>
    /// Updates a bookmark.
    /// </summary>
    Task<bool> UpdateBookmarkAsync(Bookmark bookmark);

    /// <summary>
    /// Deletes a bookmark.
    /// </summary>
    Task<bool> DeleteBookmarkAsync(Guid id);

    /// <summary>
    /// Deletes all bookmarks for a book.
    /// </summary>
    Task<bool> DeleteAllBookmarksAsync(Guid bookId);
}
