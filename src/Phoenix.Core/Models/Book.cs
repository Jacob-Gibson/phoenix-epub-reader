using System;
using System.Collections.Generic;

namespace Phoenix.Core.Models;

/// <summary>
/// Represents an ePub book in the library.
/// </summary>
public class Book
{
    /// <summary>
    /// Unique identifier for the book.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The title of the book.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The author(s) of the book.
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// The book's description or summary.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The publisher of the book.
    /// </summary>
    public string Publisher { get; set; } = string.Empty;

    /// <summary>
    /// The publication date.
    /// </summary>
    public DateTime? PublicationDate { get; set; }

    /// <summary>
    /// The ISBN of the book.
    /// </summary>
    public string Isbn { get; set; } = string.Empty;

    /// <summary>
    /// The language of the book content.
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// The file path to the ePub file.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// The cover image as a byte array.
    /// </summary>
    public byte[]? CoverImage { get; set; }

    /// <summary>
    /// The date the book was added to the library.
    /// </summary>
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The date the book was last opened.
    /// </summary>
    public DateTime? LastOpened { get; set; }

    /// <summary>
    /// The file size in bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Tags or categories assigned to the book.
    /// </summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// The table of contents for the book.
    /// </summary>
    public List<Chapter> Chapters { get; set; } = [];
}
