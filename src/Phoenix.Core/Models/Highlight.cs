using System;

namespace Phoenix.Core.Models;

/// <summary>
/// Represents a text highlight with optional annotation in a book.
/// </summary>
public class Highlight
{
    /// <summary>
    /// Unique identifier for the highlight.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The ID of the book this highlight belongs to.
    /// </summary>
    public Guid BookId { get; set; }

    /// <summary>
    /// The content file path within the ePub.
    /// </summary>
    public string ContentPath { get; set; } = string.Empty;

    /// <summary>
    /// The highlighted text content.
    /// </summary>
    public string SelectedText { get; set; } = string.Empty;

    /// <summary>
    /// The text before the selection (for context matching).
    /// </summary>
    public string TextBefore { get; set; } = string.Empty;

    /// <summary>
    /// The text after the selection (for context matching).
    /// </summary>
    public string TextAfter { get; set; } = string.Empty;

    /// <summary>
    /// The highlight color (yellow, green, blue, pink, orange).
    /// </summary>
    public HighlightColor Color { get; set; } = HighlightColor.Yellow;

    /// <summary>
    /// Optional annotation/note attached to the highlight.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// When the highlight was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the highlight was last modified.
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Available highlight colors.
/// </summary>
public enum HighlightColor
{
    Yellow = 0,
    Green = 1,
    Blue = 2,
    Pink = 3,
    Orange = 4
}
