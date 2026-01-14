using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phoenix.Core.Interfaces;
using Phoenix.Core.Models;

namespace Phoenix.UI.WPF.ViewModels;

/// <summary>
/// View model for the reader view.
/// </summary>
public partial class ReaderViewModel : ObservableObject
{
    private readonly IEpubParser _epubParser;
    private readonly IBookmarkService _bookmarkService;
    private readonly IReadingProgressService _progressService;
    private readonly MainViewModel _mainViewModel;

    /// <summary>
    /// Event to request scroll position from the view.
    /// </summary>
    public event Func<Task<double>>? RequestScrollPosition;

    [ObservableProperty]
    private Book? _currentBook;

    [ObservableProperty]
    private Chapter? _currentChapter;

    [ObservableProperty]
    private ObservableCollection<Chapter> _chapters = [];

    [ObservableProperty]
    private ObservableCollection<Bookmark> _bookmarks = [];

    [ObservableProperty]
    private string _htmlContent = string.Empty;

    [ObservableProperty]
    private string _cssContent = string.Empty;

    [ObservableProperty]
    private int _currentChapterIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScrollPositionChanged))]
    private double _scrollPosition;

    /// <summary>
    /// Used to trigger scroll in the view after navigation.
    /// </summary>
    public bool ScrollPositionChanged => true;

    [ObservableProperty]
    private double _readingProgress;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _canGoNext;

    [ObservableProperty]
    private bool _canGoPrevious;

    /// <summary>
    /// Refreshes the current chapter content with updated settings.
    /// Call this when settings like theme, font, etc. change.
    /// </summary>
    public void RefreshContent()
    {
        if (CurrentBook != null && CurrentChapter != null)
        {
            // Rebuild the HTML with current settings
            _ = LoadChapterAsync(CurrentChapterIndex);
        }
    }

    /// <summary>
    /// Called when CurrentChapter changes (from TOC selection).
    /// </summary>
    partial void OnCurrentChapterChanged(Chapter? value)
    {
        if (value != null && CurrentBook != null)
        {
            var index = Chapters.IndexOf(value);
            if (index >= 0 && index != CurrentChapterIndex)
            {
                _ = LoadChapterAsync(index);
            }
        }
    }

    public ReaderViewModel(
        IEpubParser epubParser,
        IBookmarkService bookmarkService,
        IReadingProgressService progressService,
        MainViewModel mainViewModel)
    {
        _epubParser = epubParser;
        _bookmarkService = bookmarkService;
        _progressService = progressService;
        _mainViewModel = mainViewModel;
    }

    /// <summary>
    /// Gets an image from the current book by path.
    /// </summary>
    public async Task<byte[]?> GetImageAsync(string imagePath)
    {
        if (CurrentBook == null)
        {
            return null;
        }
        
        return await _epubParser.GetImageAsync(CurrentBook.FilePath, imagePath);
    }

    public async Task OpenBookAsync(Book book)
    {
        IsLoading = true;
        CurrentBook = book;

        try
        {
            // Always re-parse the book to get fresh chapter data
            var parsedBook = await _epubParser.ParseAsync(book.FilePath);
            
            // Flatten chapters (include nested children) for easier navigation
            var flatChapters = FlattenChapters(parsedBook.Chapters);
            
            // Load chapters
            Chapters = new ObservableCollection<Chapter>(flatChapters);

            // Load CSS
            CssContent = await _epubParser.GetStylesheetsAsync(book.FilePath);

            // Load bookmarks
            var bookmarks = await _bookmarkService.GetBookmarksAsync(book.Id);
            Bookmarks = new ObservableCollection<Bookmark>(bookmarks);

            // Load reading progress
            var progress = await _progressService.GetProgressAsync(book.Id);
            
            if (progress != null && !string.IsNullOrEmpty(progress.CurrentContentPath))
            {
                // Find the chapter by content path
                var chapterIndex = FindChapterIndex(progress.CurrentContentPath);
                if (chapterIndex >= 0)
                {
                    CurrentChapterIndex = chapterIndex;
                    ScrollPosition = progress.ScrollPosition;
                }
                else
                {
                    // Start at the beginning
                    CurrentChapterIndex = 0;
                }
            }
            else
            {
                // Start at the beginning
                CurrentChapterIndex = 0;
            }

            if (Chapters.Count > 0)
            {
                await LoadChapterAsync(CurrentChapterIndex);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static List<Chapter> FlattenChapters(List<Chapter> chapters)
    {
        var result = new List<Chapter>();
        foreach (var chapter in chapters)
        {
            // Only add chapters with valid content paths
            if (!string.IsNullOrEmpty(chapter.ContentPath))
            {
                result.Add(chapter);
            }
            
            // Recursively add children
            if (chapter.Children != null && chapter.Children.Count > 0)
            {
                result.AddRange(FlattenChapters(chapter.Children));
            }
        }
        return result;
    }

    private int FindChapterIndex(string contentPath)
    {
        for (int i = 0; i < Chapters.Count; i++)
        {
            if (Chapters[i].ContentPath == contentPath)
            {
                return i;
            }
        }
        return -1;
    }

    private int FindFirstContentChapter()
    {
        // Find the first chapter that is NOT front matter
        for (int i = 0; i < Chapters.Count; i++)
        {
            if (!Chapters[i].IsFrontMatter)
            {
                return i;
            }
        }
        // If all chapters are front matter (unlikely), start at 0
        return 0;
    }

    private async Task LoadChapterAsync(int index)
    {
        if (CurrentBook == null || index < 0 || index >= Chapters.Count)
            return;

        IsLoading = true;

        try
        {
            var chapter = Chapters[index];
            CurrentChapter = chapter;
            CurrentChapterIndex = index;

            System.Diagnostics.Debug.WriteLine($"[ReaderViewModel] Loading chapter: {chapter.Title}, ContentPath: {chapter.ContentPath}");

            // Load chapter HTML content
            var rawHtml = await _epubParser.GetChapterContentAsync(CurrentBook.FilePath, chapter.ContentPath);
            
            System.Diagnostics.Debug.WriteLine($"[ReaderViewModel] Raw HTML length: {rawHtml?.Length ?? 0}");
            if (rawHtml != null && rawHtml.Length > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[ReaderViewModel] Raw HTML preview: {rawHtml.Substring(0, Math.Min(200, rawHtml.Length))}");
            }
            
            // Build full HTML with CSS
            HtmlContent = BuildHtmlDocument(rawHtml ?? "");

            System.Diagnostics.Debug.WriteLine($"[ReaderViewModel] Final HTML length: {HtmlContent?.Length ?? 0}");
            
            // Write to a log file for debugging
            var logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "phoenix_debug.html");
            System.IO.File.WriteAllText(logPath, HtmlContent ?? "EMPTY");
            System.Diagnostics.Debug.WriteLine($"[ReaderViewModel] HTML saved to: {logPath}");

            // Update navigation state
            CanGoPrevious = index > 0;
            CanGoNext = index < Chapters.Count - 1;

            // Update reading progress
            UpdateReadingProgress();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private string BuildHtmlDocument(string chapterContent)
    {
        var settings = _mainViewModel.Settings ?? new UserSettings();
        
        // Extract body content from XHTML if it's a full document
        var bodyContent = ExtractBodyContent(chapterContent);
        
        // Rewrite image URLs to use our virtual host
        bodyContent = RewriteImageUrls(bodyContent);

        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset=\"utf-8\">");
        html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        html.AppendLine("<style>");
        // First include the ePub's CSS
        html.AppendLine(CssContent);
        
        // Apply theme styles based on settings
        if (settings.Theme == ReaderTheme.Default)
        {
            // Default theme: preserve ePub's original styling, only apply layout settings
            // Set white background as fallback (ePub CSS can override this)
            html.AppendLine($@"
                html, body {{
                    background-color: #ffffff;
                    color: #000000;
                    font-family: '{settings.FontFamily}', serif;
                    font-size: {settings.FontSize}px;
                    line-height: {settings.LineHeight};
                    margin: {settings.MarginSize}px;
                    padding: 20px;
                }}
                img {{
                    max-width: 100%;
                    height: auto;
                }}
            ");
        }
        else
        {
            // Themed modes: override ePub styles with !important
            var (bgColor, textColor, linkColor) = settings.Theme switch
            {
                ReaderTheme.Dark => ("#1e1e1e", "#e0e0e0", "#6db3f2"),
                ReaderTheme.Sepia => ("#f4ecd8", "#5c4b37", "#8b6914"),
                _ => ("#ffffff", "#333333", "#0066cc") // Light
            };
            
            html.AppendLine($@"
                html, body {{
                    background-color: {bgColor} !important;
                    color: {textColor} !important;
                    font-family: '{settings.FontFamily}', serif !important;
                    font-size: {settings.FontSize}px !important;
                    line-height: {settings.LineHeight} !important;
                    margin: {settings.MarginSize}px !important;
                    padding: 20px !important;
                }}
                /* Override all text elements to use our theme color */
                body, p, div, span, h1, h2, h3, h4, h5, h6, li, td, th, blockquote, pre, code {{
                    color: {textColor} !important;
                }}
                /* Links should be slightly different */
                a {{
                    color: {linkColor} !important;
                }}
                img {{
                    max-width: 100% !important;
                    height: auto !important;
                }}
            ");
        }
        
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine(bodyContent);
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    private static string RewriteImageUrls(string html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return html;
        }

        // Rewrite relative image URLs to use our virtual host
        // Pattern: src="assets/image.png" or src="image.png" -> src="https://epub.local/assets/image.png"
        var result = html;
        
        // Use regex to find and replace img src attributes
        var imgPattern = new System.Text.RegularExpressions.Regex(
            @"(<img[^>]+src\s*=\s*[""'])([^""']+)([""'][^>]*>)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        result = imgPattern.Replace(result, match =>
        {
            var prefix = match.Groups[1].Value;
            var src = match.Groups[2].Value;
            var suffix = match.Groups[3].Value;
            
            // Skip if already absolute URL or data URI
            if (src.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                src.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                src.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return match.Value;
            }
            
            // Rewrite to virtual host
            var newSrc = $"https://epub.local/{src.TrimStart('/', '.')}";
            return $"{prefix}{newSrc}{suffix}";
        });

        return result;
    }

    private static string ExtractBodyContent(string html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return "<p>No content available</p>";
        }

        // Try to extract content between <body> tags
        var bodyStartPatterns = new[] { "<body>", "<body ", "<BODY>", "<BODY " };
        var bodyEndPatterns = new[] { "</body>", "</BODY>" };

        int bodyStart = -1;
        foreach (var pattern in bodyStartPatterns)
        {
            bodyStart = html.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (bodyStart >= 0)
            {
                // Find the closing > of the body tag
                var tagEnd = html.IndexOf('>', bodyStart);
                if (tagEnd >= 0)
                {
                    bodyStart = tagEnd + 1;
                }
                break;
            }
        }

        int bodyEnd = -1;
        foreach (var pattern in bodyEndPatterns)
        {
            bodyEnd = html.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (bodyEnd >= 0) break;
        }

        if (bodyStart >= 0 && bodyEnd > bodyStart)
        {
            return html.Substring(bodyStart, bodyEnd - bodyStart);
        }

        // If no body tags found, return the original content
        // (it might just be content without full HTML structure)
        return html;
    }

    private void UpdateReadingProgress()
    {
        if (CurrentBook == null) return;

        ReadingProgress = Chapters.Count > 0
            ? (CurrentChapterIndex + 1.0) / Chapters.Count * 100.0
            : 0;
    }

    [RelayCommand]
    private async Task NextChapterAsync()
    {
        if (CanGoNext)
        {
            await SaveProgressAsync();
            await LoadChapterAsync(CurrentChapterIndex + 1);
        }
    }

    [RelayCommand]
    private async Task PreviousChapterAsync()
    {
        if (CanGoPrevious)
        {
            await SaveProgressAsync();
            await LoadChapterAsync(CurrentChapterIndex - 1);
        }
    }

    [RelayCommand]
    private async Task GoToChapterAsync(Chapter? chapter)
    {
        if (chapter == null) return;
        
        var index = Chapters.IndexOf(chapter);
        if (index >= 0)
        {
            await SaveProgressAsync();
            await LoadChapterAsync(index);
        }
    }

    [RelayCommand]
    private async Task AddBookmarkAsync()
    {
        if (CurrentBook == null || CurrentChapter == null) return;

        // Get current scroll position from the view
        double scrollPos = 0;
        if (RequestScrollPosition != null)
        {
            scrollPos = await RequestScrollPosition.Invoke();
        }

        var bookmark = new Bookmark
        {
            BookId = CurrentBook.Id,
            ChapterId = CurrentChapter.Id,
            ContentPath = CurrentChapter.ContentPath,
            ScrollPosition = scrollPos,
            Title = $"{CurrentChapter.Title} ({scrollPos:F0}%)",
            CreatedAt = DateTime.UtcNow
        };

        await _bookmarkService.AddBookmarkAsync(bookmark);
        Bookmarks.Add(bookmark);
    }

    /// <summary>
    /// Event to request scroll to a specific position.
    /// </summary>
    public event Action<double>? ScrollToPositionRequested;

    [RelayCommand]
    private async Task GoToBookmarkAsync(Bookmark? bookmark)
    {
        if (bookmark == null) return;

        var chapterIndex = FindChapterIndex(bookmark.ContentPath);
        if (chapterIndex >= 0)
        {
            // If same chapter, just scroll; otherwise load chapter first
            if (chapterIndex == CurrentChapterIndex)
            {
                // Same chapter - just request scroll
                ScrollToPositionRequested?.Invoke(bookmark.ScrollPosition);
            }
            else
            {
                // Different chapter - load it first, then scroll
                await LoadChapterAsync(chapterIndex);
                // Small delay to let content render, then scroll
                await Task.Delay(200);
                ScrollToPositionRequested?.Invoke(bookmark.ScrollPosition);
            }
        }
    }

    [RelayCommand]
    private async Task DeleteBookmarkAsync(Bookmark? bookmark)
    {
        if (bookmark == null) return;

        await _bookmarkService.DeleteBookmarkAsync(bookmark.Id);
        Bookmarks.Remove(bookmark);
    }

    [RelayCommand]
    private async Task SaveProgressAsync()
    {
        if (CurrentBook == null || CurrentChapter == null) return;

        var progress = new ReadingProgress
        {
            BookId = CurrentBook.Id,
            CurrentChapterId = CurrentChapter.Id,
            CurrentContentPath = CurrentChapter.ContentPath,
            ScrollPosition = ScrollPosition,
            CurrentChapterIndex = CurrentChapterIndex,
            TotalChapters = Chapters.Count,
            OverallProgress = ReadingProgress
        };

        await _progressService.SaveProgressAsync(progress);
    }

    [RelayCommand]
    private async Task CloseBookAsync()
    {
        await SaveProgressAsync();
        CurrentBook = null;
        CurrentChapter = null;
        HtmlContent = string.Empty;
        _mainViewModel.ShowLibraryCommand.Execute(null);
    }

    public async Task UpdateScrollPositionAsync(double position)
    {
        ScrollPosition = position;
        await SaveProgressAsync();
    }
}
