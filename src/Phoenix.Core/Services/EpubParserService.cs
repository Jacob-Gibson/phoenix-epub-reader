using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Phoenix.Core.Interfaces;
using Phoenix.Core.Models;
using VersOne.Epub;

namespace Phoenix.Core.Services;

/// <summary>
/// Service for parsing ePub files using VersOne.Epub.
/// </summary>
public class EpubParserService : IEpubParser
{
    /// <inheritdoc />
    public async Task<Book> ParseAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("ePub file not found.", filePath);
        }

        var epubBook = await EpubReader.ReadBookAsync(filePath);
        
        var book = new Book
        {
            Title = epubBook.Title ?? Path.GetFileNameWithoutExtension(filePath),
            Author = string.Join(", ", epubBook.AuthorList ?? []),
            Description = epubBook.Description ?? string.Empty,
            FilePath = filePath,
            FileSize = new FileInfo(filePath).Length,
            DateAdded = DateTime.UtcNow
        };

        // Extract cover image
        book.CoverImage = await ExtractCoverImageAsync(epubBook);

        // Parse table of contents
        book.Chapters = ParseTableOfContents(epubBook);

        return book;
    }

    /// <inheritdoc />
    public async Task<string> GetChapterContentAsync(string filePath, string contentPath)
    {
        if (string.IsNullOrEmpty(contentPath))
        {
            return string.Empty;
        }

        var epubBook = await EpubReader.ReadBookAsync(filePath);
        var htmlFiles = epubBook.Content.Html.Local.ToList();
        
        if (htmlFiles.Count == 0)
        {
            return "<p>No content available</p>";
        }

        EpubLocalTextContentFile? contentFile = null;
        
        // Normalize the content path - remove any fragment (#anchor)
        var normalizedPath = contentPath.Contains('#') 
            ? contentPath.Substring(0, contentPath.IndexOf('#')) 
            : contentPath;

        // First, try exact match on FilePath
        contentFile = htmlFiles
            .FirstOrDefault(f => !string.IsNullOrEmpty(f.FilePath) && 
                                  f.FilePath.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase));

        // Try EndsWith match (for relative paths)
        if (contentFile == null)
        {
            contentFile = htmlFiles
                .FirstOrDefault(f => !string.IsNullOrEmpty(f.FilePath) && 
                                      f.FilePath.EndsWith(normalizedPath, StringComparison.OrdinalIgnoreCase));
        }

        // Try if contentPath ends with the file path
        if (contentFile == null)
        {
            contentFile = htmlFiles
                .FirstOrDefault(f => !string.IsNullOrEmpty(f.FilePath) && 
                                      normalizedPath.EndsWith(f.FilePath, StringComparison.OrdinalIgnoreCase));
        }

        // Try to find by filename only
        if (contentFile == null)
        {
            var fileName = Path.GetFileName(normalizedPath);
            if (!string.IsNullOrEmpty(fileName))
            {
                contentFile = htmlFiles
                    .FirstOrDefault(f => !string.IsNullOrEmpty(f.FilePath) &&
                                          Path.GetFileName(f.FilePath)
                                              .Equals(fileName, StringComparison.OrdinalIgnoreCase));
            }
        }

        // Try partial match on filename without extension
        if (contentFile == null)
        {
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(normalizedPath);
            if (!string.IsNullOrEmpty(fileNameWithoutExt))
            {
                contentFile = htmlFiles
                    .FirstOrDefault(f => !string.IsNullOrEmpty(f.FilePath) &&
                                          Path.GetFileNameWithoutExtension(f.FilePath)
                                              .Equals(fileNameWithoutExt, StringComparison.OrdinalIgnoreCase));
            }
        }

        // Last resort: return first HTML file
        if (contentFile == null)
        {
            contentFile = htmlFiles.FirstOrDefault();
        }

        var htmlContent = contentFile?.Content ?? "<p>Content not found</p>";
        
        // Note: Image embedding is disabled due to WebView2 size limits
        // Images will be loaded via virtual host mapping in the UI layer
        
        return htmlContent;
    }

    /// <summary>
    /// Gets all images from the ePub as a dictionary of path to base64 data URI.
    /// </summary>
    public async Task<Dictionary<string, string>> GetImagesAsBase64Async(string filePath)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var epubBook = await EpubReader.ReadBookAsync(filePath);
        
        foreach (var image in epubBook.Content.Images.Local)
        {
            if (image.Content == null || string.IsNullOrEmpty(image.FilePath))
            {
                continue;
            }

            var fileName = Path.GetFileName(image.FilePath);
            var base64 = Convert.ToBase64String(image.Content);
            var mimeType = GetImageMimeType(image.FilePath);
            var dataUri = $"data:{mimeType};base64,{base64}";
            
            result[fileName] = dataUri;
            result[image.FilePath] = dataUri;
            
            if (image.FilePath.StartsWith("EPUB/", StringComparison.OrdinalIgnoreCase))
            {
                var relativePath = image.FilePath.Substring(5);
                result[relativePath] = dataUri;
            }
        }

        return result;
    }

    private static string GetImageMimeType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            _ => "image/png"
        };
    }

    /// <inheritdoc />
    public async Task<string> GetStylesheetsAsync(string filePath)
    {
        var epubBook = await EpubReader.ReadBookAsync(filePath);
        var cssBuilder = new StringBuilder();

        foreach (var css in epubBook.Content.Css.Local)
        {
            cssBuilder.AppendLine(css.Content);
        }

        return cssBuilder.ToString();
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetImageAsync(string filePath, string imagePath)
    {
        var epubBook = await EpubReader.ReadBookAsync(filePath);
        
        // Normalize the image path
        var normalizedPath = imagePath.TrimStart('/', '.');
        var fileName = Path.GetFileName(normalizedPath);
        
        // Try multiple matching strategies
        var imageFile = epubBook.Content.Images.Local
            .FirstOrDefault(f => 
                // Exact match on file path
                f.FilePath.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase) ||
                f.FilePath.Equals($"EPUB/{normalizedPath}", StringComparison.OrdinalIgnoreCase) ||
                // Match on filename only
                Path.GetFileName(f.FilePath).Equals(fileName, StringComparison.OrdinalIgnoreCase) ||
                // EndsWith for relative paths
                f.FilePath.EndsWith(normalizedPath, StringComparison.OrdinalIgnoreCase) ||
                f.FilePath.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

        return imageFile?.Content;
    }

    private static async Task<byte[]?> ExtractCoverImageAsync(EpubBook epubBook)
    {
        // Try to get cover from the Cover property first
        var coverImage = epubBook.CoverImage;
        if (coverImage != null)
        {
            return await Task.FromResult(coverImage);
        }

        // Fallback: look for common cover image names
        var coverNames = new[] { "cover", "cover-image", "coverimage" };
        
        foreach (var image in epubBook.Content.Images.Local)
        {
            var fileName = Path.GetFileNameWithoutExtension(image.FilePath).ToLowerInvariant();
            if (coverNames.Any(cn => fileName.Contains(cn)))
            {
                return image.Content;
            }
        }

        return null;
    }

    private static List<Chapter> ParseTableOfContents(EpubBook epubBook)
    {
        var chapters = new List<Chapter>();
        var navigation = epubBook.Navigation;

        if (navigation != null && navigation.Any())
        {
            // Parse navigation items recursively
            int chapterOrder = 0;
            foreach (var navItem in navigation)
            {
                // If this nav item has no content path but has nested items,
                // add the nested items directly (common for "Table of Contents" wrapper)
                if (string.IsNullOrEmpty(navItem.Link?.ContentFilePath) && 
                    navItem.NestedItems != null && navItem.NestedItems.Any())
                {
                    foreach (var nestedItem in navItem.NestedItems)
                    {
                        var nestedChapter = ParseNavigationItem(nestedItem, ref chapterOrder, 0);
                        if (!string.IsNullOrEmpty(nestedChapter.ContentPath))
                        {
                            chapters.Add(nestedChapter);
                        }
                    }
                }
                else
                {
                    var chapter = ParseNavigationItem(navItem, ref chapterOrder, 0);
                    if (!string.IsNullOrEmpty(chapter.ContentPath))
                    {
                        chapters.Add(chapter);
                    }
                }
            }
        }

        // If no valid chapters from navigation, fallback to reading order
        if (chapters.Count == 0 && epubBook.ReadingOrder != null)
        {
            int order = 0;
            foreach (var item in epubBook.ReadingOrder)
            {
                if (!string.IsNullOrEmpty(item.FilePath))
                {
                    chapters.Add(new Chapter
                    {
                        Title = $"Chapter {order + 1}",
                        ContentPath = item.FilePath,
                        Order = order++,
                        Level = 0
                    });
                }
            }
        }

        // Last fallback: use HTML content files directly
        if (chapters.Count == 0)
        {
            int order = 0;
            foreach (var html in epubBook.Content.Html.Local)
            {
                chapters.Add(new Chapter
                {
                    Title = Path.GetFileNameWithoutExtension(html.FilePath) ?? $"Section {order + 1}",
                    ContentPath = html.FilePath,
                    Order = order++,
                    Level = 0
                });
            }
        }

        return chapters;
    }

    private static Chapter ParseNavigationItem(EpubNavigationItem navItem, ref int order, int level)
    {
        var title = navItem.Title ?? "Untitled";
        var contentPath = navItem.Link?.ContentFilePath ?? string.Empty;
        
        var chapter = new Chapter
        {
            Title = title,
            ContentPath = contentPath,
            Anchor = navItem.Link?.Anchor,
            Order = order++,
            Level = level,
            IsFrontMatter = IsFrontMatterContent(title, contentPath)
        };

        if (navItem.NestedItems != null)
        {
            foreach (var childItem in navItem.NestedItems)
            {
                var childChapter = ParseNavigationItem(childItem, ref order, level + 1);
                if (!string.IsNullOrEmpty(childChapter.ContentPath))
                {
                    chapter.Children.Add(childChapter);
                }
            }
        }

        return chapter;
    }

    private static bool IsFrontMatterContent(string title, string contentPath)
    {
        var lowerTitle = title.ToLowerInvariant();
        var lowerPath = contentPath.ToLowerInvariant();
        
        // Common front matter patterns
        var frontMatterKeywords = new[]
        {
            "cover", "title", "copyright", "table of contents", "toc",
            "dedication", "acknowledgement", "acknowledgment", "preface",
            "foreword", "introduction", "prologue", "epigraph",
            "publisher", "about", "front-matter", "frontmatter"
        };

        return frontMatterKeywords.Any(keyword => 
            lowerTitle.Contains(keyword) || lowerPath.Contains(keyword));
    }
}
