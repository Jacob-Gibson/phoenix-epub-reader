using System;

namespace Phoenix.Core.Models;

/// <summary>
/// Represents the reading progress for a book.
/// </summary>
public class ReadingProgress
{
    /// <summary>
    /// Unique identifier for the reading progress record.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The ID of the book.
    /// </summary>
    public Guid BookId { get; set; }

    /// <summary>
    /// The current chapter ID.
    /// </summary>
    public Guid? CurrentChapterId { get; set; }

    /// <summary>
    /// The content file path of the current position.
    /// </summary>
    public string CurrentContentPath { get; set; } = string.Empty;

    /// <summary>
    /// The scroll position within the current chapter (0-100 percentage).
    /// </summary>
    public double ScrollPosition { get; set; }

    /// <summary>
    /// The overall reading progress as a percentage (0-100).
    /// </summary>
    public double OverallProgress { get; set; }

    /// <summary>
    /// The current chapter index (0-based).
    /// </summary>
    public int CurrentChapterIndex { get; set; }

    /// <summary>
    /// The total number of chapters.
    /// </summary>
    public int TotalChapters { get; set; }

    /// <summary>
    /// The last time this progress was updated.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Total time spent reading this book (in seconds).
    /// </summary>
    public long TotalReadingTimeSeconds { get; set; }
}
