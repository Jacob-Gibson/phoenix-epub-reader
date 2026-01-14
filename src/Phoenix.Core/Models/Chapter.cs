using System;
using System.Collections.Generic;

namespace Phoenix.Core.Models;

/// <summary>
/// Represents a chapter or section in an ePub book.
/// </summary>
public class Chapter
{
    /// <summary>
    /// Unique identifier for the chapter.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The title of the chapter.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The content file path within the ePub (e.g., "OEBPS/chapter1.xhtml").
    /// </summary>
    public string ContentPath { get; set; } = string.Empty;

    /// <summary>
    /// The anchor within the content file (for sub-sections).
    /// </summary>
    public string? Anchor { get; set; }

    /// <summary>
    /// The order of the chapter in the book's spine.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// The nesting level (0 for top-level chapters, 1 for sub-chapters, etc.).
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Indicates if this is front matter (cover, title page, copyright, etc.).
    /// </summary>
    public bool IsFrontMatter { get; set; }

    /// <summary>
    /// Child chapters (for hierarchical TOC).
    /// </summary>
    public List<Chapter> Children { get; set; } = [];
}
