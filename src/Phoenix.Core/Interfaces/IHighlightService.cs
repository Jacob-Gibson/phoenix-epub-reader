using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Phoenix.Core.Models;

namespace Phoenix.Core.Interfaces;

/// <summary>
/// Service for managing text highlights and annotations.
/// </summary>
public interface IHighlightService
{
    /// <summary>
    /// Gets all highlights for a specific book.
    /// </summary>
    Task<IEnumerable<Highlight>> GetHighlightsAsync(Guid bookId);

    /// <summary>
    /// Gets all highlights for a specific chapter in a book.
    /// </summary>
    Task<IEnumerable<Highlight>> GetHighlightsForChapterAsync(Guid bookId, string contentPath);

    /// <summary>
    /// Gets a highlight by ID.
    /// </summary>
    Task<Highlight?> GetHighlightByIdAsync(Guid id);

    /// <summary>
    /// Adds a new highlight.
    /// </summary>
    Task<Highlight> AddHighlightAsync(Highlight highlight);

    /// <summary>
    /// Updates an existing highlight (e.g., changing color or note).
    /// </summary>
    Task<bool> UpdateHighlightAsync(Highlight highlight);

    /// <summary>
    /// Deletes a highlight.
    /// </summary>
    Task<bool> DeleteHighlightAsync(Guid id);

    /// <summary>
    /// Deletes all highlights for a book.
    /// </summary>
    Task<bool> DeleteAllHighlightsAsync(Guid bookId);
}
