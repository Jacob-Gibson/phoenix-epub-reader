using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Phoenix.Core.Models;

namespace Phoenix.Core.Interfaces;

/// <summary>
/// Interface for managing the book library.
/// </summary>
public interface ILibraryService
{
    /// <summary>
    /// Gets all books in the library.
    /// </summary>
    Task<IEnumerable<Book>> GetAllBooksAsync();

    /// <summary>
    /// Gets a book by its ID.
    /// </summary>
    Task<Book?> GetBookByIdAsync(Guid id);

    /// <summary>
    /// Gets a book by its file path.
    /// </summary>
    Task<Book?> GetBookByPathAsync(string filePath);

    /// <summary>
    /// Adds a book to the library.
    /// </summary>
    Task<Book> AddBookAsync(Book book);

    /// <summary>
    /// Updates a book in the library.
    /// </summary>
    Task<bool> UpdateBookAsync(Book book);

    /// <summary>
    /// Removes a book from the library.
    /// </summary>
    Task<bool> DeleteBookAsync(Guid id);

    /// <summary>
    /// Searches books by title or author.
    /// </summary>
    Task<IEnumerable<Book>> SearchBooksAsync(string query);

    /// <summary>
    /// Gets recently opened books.
    /// </summary>
    Task<IEnumerable<Book>> GetRecentBooksAsync(int count = 10);

    /// <summary>
    /// Imports an ePub file into the library.
    /// </summary>
    Task<Book> ImportBookAsync(string filePath);
}
