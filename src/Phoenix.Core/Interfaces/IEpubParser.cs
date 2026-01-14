using System.Threading.Tasks;
using Phoenix.Core.Models;

namespace Phoenix.Core.Interfaces;

/// <summary>
/// Interface for parsing ePub files.
/// </summary>
public interface IEpubParser
{
    /// <summary>
    /// Parses an ePub file and returns book metadata and structure.
    /// </summary>
    /// <param name="filePath">The path to the ePub file.</param>
    /// <returns>A Book object with metadata, chapters, and cover image.</returns>
    Task<Book> ParseAsync(string filePath);

    /// <summary>
    /// Gets the HTML content of a specific chapter.
    /// </summary>
    /// <param name="filePath">The path to the ePub file.</param>
    /// <param name="contentPath">The content path within the ePub.</param>
    /// <returns>The HTML content as a string.</returns>
    Task<string> GetChapterContentAsync(string filePath, string contentPath);

    /// <summary>
    /// Gets all CSS stylesheets from the ePub.
    /// </summary>
    /// <param name="filePath">The path to the ePub file.</param>
    /// <returns>Combined CSS content.</returns>
    Task<string> GetStylesheetsAsync(string filePath);

    /// <summary>
    /// Gets an image from the ePub by its content path.
    /// </summary>
    /// <param name="filePath">The path to the ePub file.</param>
    /// <param name="imagePath">The image path within the ePub.</param>
    /// <returns>The image as a byte array.</returns>
    Task<byte[]?> GetImageAsync(string filePath, string imagePath);
}
