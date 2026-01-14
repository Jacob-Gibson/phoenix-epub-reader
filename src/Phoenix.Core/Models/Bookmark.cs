using System;

namespace Phoenix.Core.Models;

/// <summary>
/// Represents a bookmark in a book.
/// </summary>
public class Bookmark
{
    /// <summary>
    /// Unique identifier for the bookmark.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The ID of the book this bookmark belongs to.
    /// </summary>
    public Guid BookId { get; set; }

    /// <summary>
    /// The chapter ID where the bookmark is located.
    /// </summary>
    public Guid ChapterId { get; set; }

    /// <summary>
    /// The content file path within the ePub.
    /// </summary>
    public string ContentPath { get; set; } = string.Empty;

    /// <summary>
    /// The CSS selector or element ID to scroll to.
    /// </summary>
    public string? ElementSelector { get; set; }

    /// <summary>
    /// The scroll position as a percentage (0-100).
    /// </summary>
    public double ScrollPosition { get; set; }

    /// <summary>
    /// User-defined title for the bookmark.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional note attached to the bookmark.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// The date the bookmark was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// A snippet of text around the bookmark location.
    /// </summary>
    public string? TextSnippet { get; set; }
}
