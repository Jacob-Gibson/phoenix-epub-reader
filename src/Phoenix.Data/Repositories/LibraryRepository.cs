using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Phoenix.Core.Interfaces;
using Phoenix.Core.Models;

namespace Phoenix.Data.Repositories;

/// <summary>
/// LiteDB implementation of the library service.
/// </summary>
public class LibraryRepository : ILibraryService
{
    private readonly PhoenixDatabase _database;
    private readonly IEpubParser _epubParser;

    public LibraryRepository(PhoenixDatabase database, IEpubParser epubParser)
    {
        _database = database;
        _epubParser = epubParser;
    }

    /// <inheritdoc />
    public Task<IEnumerable<Book>> GetAllBooksAsync()
    {
        var books = _database.Books.FindAll().ToList();
        return Task.FromResult<IEnumerable<Book>>(books);
    }

    /// <inheritdoc />
    public Task<Book?> GetBookByIdAsync(Guid id)
    {
        var book = _database.Books.FindOne(b => b.Id == id);
        return Task.FromResult<Book?>(book);
    }

    /// <inheritdoc />
    public Task<Book?> GetBookByPathAsync(string filePath)
    {
        var book = _database.Books.FindOne(b => b.FilePath == filePath);
        return Task.FromResult<Book?>(book);
    }

    /// <inheritdoc />
    public Task<Book> AddBookAsync(Book book)
    {
        _database.Books.Insert(book);
        return Task.FromResult(book);
    }

    /// <inheritdoc />
    public Task<bool> UpdateBookAsync(Book book)
    {
        var result = _database.Books.Update(book);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<bool> DeleteBookAsync(Guid id)
    {
        // Also delete related data
        _database.Bookmarks.DeleteMany(bm => bm.BookId == id);
        _database.ReadingProgress.DeleteMany(rp => rp.BookId == id);
        
        var result = _database.Books.Delete(id);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IEnumerable<Book>> SearchBooksAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return GetAllBooksAsync();
        }

        var lowerQuery = query.ToLowerInvariant();
        var books = _database.Books
            .Find(b => b.Title.ToLower().Contains(lowerQuery) || 
                       b.Author.ToLower().Contains(lowerQuery))
            .ToList();
        
        return Task.FromResult<IEnumerable<Book>>(books);
    }

    /// <inheritdoc />
    public Task<IEnumerable<Book>> GetRecentBooksAsync(int count = 10)
    {
        var books = _database.Books
            .Find(b => b.LastOpened != null)
            .OrderByDescending(b => b.LastOpened)
            .Take(count)
            .ToList();
        
        return Task.FromResult<IEnumerable<Book>>(books);
    }

    /// <inheritdoc />
    public async Task<Book> ImportBookAsync(string filePath)
    {
        // Check if book already exists
        var existingBook = await GetBookByPathAsync(filePath);
        if (existingBook != null)
        {
            return existingBook;
        }

        // Parse the ePub file
        var book = await _epubParser.ParseAsync(filePath);
        
        // Save to database
        await AddBookAsync(book);
        
        return book;
    }
}
