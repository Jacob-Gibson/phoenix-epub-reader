using System;
using System.Threading.Tasks;
using Phoenix.Core.Models;

namespace Phoenix.Core.Interfaces;

/// <summary>
/// Interface for managing reading progress.
/// </summary>
public interface IReadingProgressService
{
    /// <summary>
    /// Gets the reading progress for a book.
    /// </summary>
    Task<ReadingProgress?> GetProgressAsync(Guid bookId);

    /// <summary>
    /// Saves or updates reading progress.
    /// </summary>
    Task<ReadingProgress> SaveProgressAsync(ReadingProgress progress);

    /// <summary>
    /// Deletes reading progress for a book.
    /// </summary>
    Task<bool> DeleteProgressAsync(Guid bookId);
}
