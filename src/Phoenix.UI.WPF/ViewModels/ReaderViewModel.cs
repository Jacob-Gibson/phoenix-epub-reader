using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
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
    private readonly IHighlightService _highlightService;
    private readonly MainViewModel _mainViewModel;

    /// <summary>
    /// Event to request scroll position from the view.
    /// </summary>
    public event Func<Task<double>>? RequestScrollPosition;

    /// <summary>
    /// Event to scroll to a specific position.
    /// </summary>
    public event Action<double>? ScrollToPositionRequested;

    /// <summary>
    /// Event to apply highlights to the current page.
    /// </summary>
    public event Action<string>? ApplyHighlightsRequested;

    [ObservableProperty]
    private Book? _currentBook;

    [ObservableProperty]
    private Chapter? _currentChapter;

    [ObservableProperty]
    private ObservableCollection<Chapter> _chapters = [];

    [ObservableProperty]
    private ObservableCollection<Bookmark> _bookmarks = [];

    [ObservableProperty]
    private ObservableCollection<Highlight> _highlights = [];

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
        IHighlightService highlightService,
        MainViewModel mainViewModel)
    {
        _epubParser = epubParser;
        _bookmarkService = bookmarkService;
        _progressService = progressService;
        _highlightService = highlightService;
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

            // Load highlights for this book
            var highlights = await _highlightService.GetHighlightsAsync(book.Id);
            Highlights = new ObservableCollection<Highlight>(highlights);

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

            // Load chapter HTML content
            var rawHtml = await _epubParser.GetChapterContentAsync(CurrentBook.FilePath, chapter.ContentPath);
            
            // Build full HTML with CSS
            HtmlContent = BuildHtmlDocument(rawHtml ?? "");

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
        
        // Apply theme styles using template
        html.AppendLine(GenerateThemeCss(settings));
        
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine(bodyContent);
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    /// <summary>
    /// Generates theme CSS using a consistent template for all themes.
    /// </summary>
    private static string GenerateThemeCss(UserSettings settings)
    {
        // Theme-specific colors
        var (bgColor, textColor, linkColor, useImportant) = settings.Theme switch
        {
            ReaderTheme.Dark => ("#1e1e1e", "#e0e0e0", "#6db3f2", true),
            ReaderTheme.Sepia => ("#f4ecd8", (string?)null, (string?)null, false), // null = preserve ePub colors
            _ => ("#ffffff", (string?)null, (string?)null, false) // Default
        };

        var important = useImportant ? " !important" : "";
        
        var css = new StringBuilder();
        
        // Base layout styles - applied to all themes
        css.AppendLine($@"
            html, body {{
                background-color: {bgColor}{important};
                font-family: '{settings.FontFamily}', serif{important};
                font-size: {settings.FontSize}px{important};
                line-height: {settings.LineHeight}{important};
                margin: {settings.MarginSize}px{important};
                padding: 20px{important};
                word-wrap: break-word{important};
                overflow-wrap: break-word{important};
            }}
        ");

        // Text color - only applied if specified (Dark mode)
        if (textColor != null)
        {
            css.AppendLine($@"
                html, body {{
                    color: {textColor}{important};
                }}
                /* Override all text elements */
                body, p, div, span, h1, h2, h3, h4, h5, h6, li, td, th, blockquote, pre, code,
                article, section, header, footer, aside, nav, main, figure, figcaption,
                strong, em, b, i, u, s, sub, sup, small, mark, cite, q, abbr, address,
                dl, dt, dd, ol, ul, label, legend, caption, summary, details {{
                    color: {textColor}{important};
                }}
            ");
        }

        // Link color - only applied if specified (Dark mode)
        if (linkColor != null)
        {
            css.AppendLine($@"
                a {{
                    color: {linkColor}{important};
                }}
            ");
        }

        // Image styles - consistent across all themes
        css.AppendLine($@"
            img {{
                max-width: 100%{important};
                height: auto{important};
                display: block{important};
            }}
        ");

        return css.ToString();
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

    #region Highlighting

    /// <summary>
    /// Adds a highlight for selected text.
    /// </summary>
    [RelayCommand]
    private async Task AddHighlightAsync(HighlightData? data)
    {
        if (data == null || CurrentBook == null || CurrentChapter == null) return;

        var highlight = new Highlight
        {
            BookId = CurrentBook.Id,
            ContentPath = CurrentChapter.ContentPath,
            SelectedText = data.SelectedText,
            TextBefore = data.TextBefore,
            TextAfter = data.TextAfter,
            Color = data.Color,
            Note = data.Note ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        await _highlightService.AddHighlightAsync(highlight);
        Highlights.Add(highlight);

        // Notify view to apply highlights
        NotifyHighlightsChanged();
        OnPropertyChanged(nameof(CurrentChapterHighlightCount));
        OnPropertyChanged(nameof(CurrentBookHighlightCount));
    }

    /// <summary>
    /// Updates an existing highlight's note or color.
    /// </summary>
    [RelayCommand]
    private async Task UpdateHighlightAsync(Highlight? highlight)
    {
        if (highlight == null) return;

        highlight.ModifiedAt = DateTime.UtcNow;
        await _highlightService.UpdateHighlightAsync(highlight);

        // Update in collection
        var index = Highlights.ToList().FindIndex(h => h.Id == highlight.Id);
        if (index >= 0)
        {
            Highlights[index] = highlight;
        }

        NotifyHighlightsChanged();
    }

    /// <summary>
    /// Deletes a highlight.
    /// </summary>
    [RelayCommand]
    private async Task DeleteHighlightAsync(Highlight? highlight)
    {
        if (highlight == null) return;

        await _highlightService.DeleteHighlightAsync(highlight.Id);
        
        var toRemove = Highlights.FirstOrDefault(h => h.Id == highlight.Id);
        if (toRemove != null)
        {
            Highlights.Remove(toRemove);
        }

        NotifyHighlightsChanged();
    }

    /// <summary>
    /// Gets highlights for the current chapter.
    /// </summary>
    public IEnumerable<Highlight> GetCurrentChapterHighlights()
    {
        if (CurrentBook == null || CurrentChapter == null)
        {
            return Enumerable.Empty<Highlight>();
        }

        return Highlights.Where(h => 
            h.BookId == CurrentBook.Id && 
            h.ContentPath == CurrentChapter.ContentPath);
    }

    /// <summary>
    /// Gets highlights JSON for JavaScript.
    /// </summary>
    public string GetHighlightsJson()
    {
        var highlights = GetCurrentChapterHighlights().Select(h => new
        {
            id = h.Id,
            text = h.SelectedText ?? "",
            textBefore = h.TextBefore ?? "",
            textAfter = h.TextAfter ?? "",
            color = GetHighlightColorHex(h.Color),
            note = h.Note ?? ""
        }).ToList();

        return JsonSerializer.Serialize(highlights);
    }

    private static string GetHighlightColorHex(HighlightColor color)
    {
        return color switch
        {
            HighlightColor.Yellow => "#ffeb3b80",
            HighlightColor.Green => "#4caf5080",
            HighlightColor.Blue => "#2196f380",
            HighlightColor.Pink => "#e91e6380",
            HighlightColor.Orange => "#ff980080",
            _ => "#ffeb3b80"
        };
    }

    private void NotifyHighlightsChanged()
    {
        var json = GetHighlightsJson();
        ApplyHighlightsRequested?.Invoke(json);
    }

    #endregion

    #region Debug Mode

    /// <summary>
    /// Gets whether debug mode is enabled from settings.
    /// </summary>
    public bool IsDebugModeEnabled => _mainViewModel.Settings?.DebugModeEnabled ?? false;

    /// <summary>
    /// Gets the count of highlights in the current chapter.
    /// </summary>
    public int CurrentChapterHighlightCount => GetCurrentChapterHighlights().Count();

    /// <summary>
    /// Gets the count of highlights in the current book.
    /// </summary>
    public int CurrentBookHighlightCount => CurrentBook == null 
        ? 0 
        : Highlights.Count(h => h.BookId == CurrentBook.Id);

    /// <summary>
    /// Last validation report message.
    /// </summary>
    [ObservableProperty]
    private string _validationReport = string.Empty;

    /// <summary>
    /// Refreshes highlights for the current chapter.
    /// </summary>
    [RelayCommand]
    private void RefreshHighlights()
    {
        NotifyHighlightsChanged();
        OnPropertyChanged(nameof(CurrentChapterHighlightCount));
        OnPropertyChanged(nameof(CurrentBookHighlightCount));
    }

    /// <summary>
    /// Event to request a JavaScript test from the view.
    /// </summary>
    public event Action<string>? TestJavaScriptRequested;

    /// <summary>
    /// Tests the JavaScript highlight rendering directly.
    /// </summary>
    [RelayCommand]
    private void TestHighlightRendering()
    {
        var json = GetHighlightsJson();
        TestJavaScriptRequested?.Invoke(json);
    }

    /// <summary>
    /// Shows details about current highlights for debugging.
    /// </summary>
    [RelayCommand]
    private void ShowHighlightDebugInfo()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"CurrentBook: {CurrentBook?.Title ?? "null"} (ID: {CurrentBook?.Id})");
        sb.AppendLine($"CurrentChapter: {CurrentChapter?.Title ?? "null"} (Path: {CurrentChapter?.ContentPath})");
        sb.AppendLine($"Total Highlights in memory: {Highlights.Count}");
        sb.AppendLine($"GetCurrentChapterHighlights count: {GetCurrentChapterHighlights().Count()}");
        sb.AppendLine($"GetHighlightsJson: {GetHighlightsJson()}");
        sb.AppendLine();
        
        foreach (var h in Highlights.Take(10))
        {
            var text = h.SelectedText ?? "";
            var textBefore = h.TextBefore ?? "";
            sb.AppendLine($"--- Highlight {h.Id} ---");
            sb.AppendLine($"  BookId: {h.BookId}");
            sb.AppendLine($"  ContentPath: {h.ContentPath}");
            sb.AppendLine($"  Text: {(text.Length > 50 ? text.Substring(0, 50) + "..." : text)}");
            sb.AppendLine($"  TextBefore: '{textBefore}' (len={textBefore.Length})");
            sb.AppendLine($"  Match current chapter: {h.BookId == CurrentBook?.Id && h.ContentPath == CurrentChapter?.ContentPath}");
            sb.AppendLine();
        }
        
        if (Highlights.Count > 10)
            sb.AppendLine($"... and {Highlights.Count - 10} more");
        
        ShowValidationDialog("Highlight Debug Info", sb.ToString());
    }

    /// <summary>
    /// Removes highlights that have invalid or missing data.
    /// </summary>
    [RelayCommand]
    private async Task FlushInvalidHighlightsAsync()
    {
        if (CurrentBook == null) return;

        var invalidHighlights = Highlights.Where(h =>
            string.IsNullOrWhiteSpace(h.SelectedText) ||
            h.BookId == Guid.Empty ||
            string.IsNullOrWhiteSpace(h.ContentPath) ||
            h.SelectedText.Length < 1 ||
            h.SelectedText.Length > 10000 || // Suspiciously long
            h.Id == Guid.Empty
        ).ToList();

        int flushedCount = 0;
        foreach (var highlight in invalidHighlights)
        {
            try
            {
                await _highlightService.DeleteHighlightAsync(highlight.Id);
                Highlights.Remove(highlight);
                flushedCount++;
            }
            catch
            {
                // Continue with next highlight
            }
        }

        NotifyHighlightsChanged();
        OnPropertyChanged(nameof(CurrentChapterHighlightCount));
        OnPropertyChanged(nameof(CurrentBookHighlightCount));

        ValidationReport = $"Flushed {flushedCount} invalid highlight(s).";
        ShowValidationDialog("Flush Complete", ValidationReport);
    }

    /// <summary>
    /// Clears all highlights in the current chapter.
    /// </summary>
    [RelayCommand]
    private async Task ClearChapterHighlightsAsync()
    {
        if (CurrentBook == null || CurrentChapter == null) return;

        var chapterHighlights = GetCurrentChapterHighlights().ToList();
        
        if (chapterHighlights.Count == 0)
        {
            ShowValidationDialog("Clear Chapter", "No highlights in this chapter.");
            return;
        }

        // Confirm action
        var result = System.Windows.MessageBox.Show(
            $"Delete {chapterHighlights.Count} highlight(s) from this chapter?\n\nThis cannot be undone.",
            "Clear Chapter Highlights",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        foreach (var highlight in chapterHighlights)
        {
            await _highlightService.DeleteHighlightAsync(highlight.Id);
            Highlights.Remove(highlight);
        }

        NotifyHighlightsChanged();
        OnPropertyChanged(nameof(CurrentChapterHighlightCount));
        OnPropertyChanged(nameof(CurrentBookHighlightCount));
    }

    /// <summary>
    /// Clears all highlights in the current book.
    /// </summary>
    [RelayCommand]
    private async Task ClearBookHighlightsAsync()
    {
        if (CurrentBook == null) return;

        var bookHighlights = Highlights.Where(h => h.BookId == CurrentBook.Id).ToList();
        
        if (bookHighlights.Count == 0)
        {
            ShowValidationDialog("Clear Book", "No highlights in this book.");
            return;
        }

        // Confirm action
        var result = System.Windows.MessageBox.Show(
            $"Delete ALL {bookHighlights.Count} highlight(s) from this book?\n\nThis cannot be undone!",
            "Clear All Book Highlights",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        foreach (var highlight in bookHighlights)
        {
            await _highlightService.DeleteHighlightAsync(highlight.Id);
            Highlights.Remove(highlight);
        }

        NotifyHighlightsChanged();
        OnPropertyChanged(nameof(CurrentChapterHighlightCount));
        OnPropertyChanged(nameof(CurrentBookHighlightCount));
    }

    /// <summary>
    /// Validates all highlights and generates a report.
    /// </summary>
    [RelayCommand]
    private void ValidateHighlights()
    {
        var issues = new System.Text.StringBuilder();
        int issueCount = 0;

        foreach (var highlight in Highlights)
        {
            var highlightIssues = new System.Collections.Generic.List<string>();

            // Check for missing/invalid data
            if (highlight.Id == Guid.Empty)
                highlightIssues.Add("Missing ID");
            if (highlight.BookId == Guid.Empty)
                highlightIssues.Add("Missing BookId");
            if (string.IsNullOrWhiteSpace(highlight.ContentPath))
                highlightIssues.Add("Missing ContentPath");
            if (string.IsNullOrWhiteSpace(highlight.SelectedText))
                highlightIssues.Add("Empty SelectedText");
            if (highlight.SelectedText?.Length > 5000)
                highlightIssues.Add($"Very long text ({highlight.SelectedText.Length} chars)");
            if (string.IsNullOrEmpty(highlight.TextBefore) && string.IsNullOrEmpty(highlight.TextAfter))
                highlightIssues.Add("No context (may fail to locate)");
            if (highlight.CreatedAt == default)
                highlightIssues.Add("Missing CreatedAt");

            if (highlightIssues.Count > 0)
            {
                issueCount++;
                var preview = highlight.SelectedText?.Length > 30 
                    ? highlight.SelectedText.Substring(0, 30) + "..." 
                    : highlight.SelectedText ?? "(empty)";
                issues.AppendLine($"• \"{preview}\"");
                foreach (var issue in highlightIssues)
                {
                    issues.AppendLine($"   - {issue}");
                }
                issues.AppendLine();
            }
        }

        if (issueCount == 0)
        {
            ValidationReport = $"✓ All {Highlights.Count} highlight(s) passed validation.";
        }
        else
        {
            ValidationReport = $"Found {issueCount} highlight(s) with issues:\n\n{issues}";
        }

        ShowValidationDialog("Validation Report", ValidationReport);
    }

    private static void ShowValidationDialog(string title, string message)
    {
        var dialog = new System.Windows.Window
        {
            Title = title,
            Width = 450,
            Height = 350,
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen
        };

        var panel = new System.Windows.Controls.StackPanel { Margin = new System.Windows.Thickness(15) };
        
        var scrollViewer = new System.Windows.Controls.ScrollViewer 
        { 
            MaxHeight = 250,
            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto
        };
        
        scrollViewer.Content = new System.Windows.Controls.TextBlock 
        { 
            Text = message,
            TextWrapping = System.Windows.TextWrapping.Wrap,
            FontFamily = new System.Windows.Media.FontFamily("Consolas")
        };
        
        panel.Children.Add(scrollViewer);

        var okButton = new System.Windows.Controls.Button 
        { 
            Content = "OK", 
            Width = 80,
            Margin = new System.Windows.Thickness(0, 15, 0, 0),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        };
        okButton.Click += (s, e) => dialog.Close();
        panel.Children.Add(okButton);

        dialog.Content = panel;
        dialog.ShowDialog();
    }

    #endregion
}

/// <summary>
/// Data class for passing highlight information from JavaScript.
/// </summary>
public class HighlightData
{
    public string SelectedText { get; set; } = string.Empty;
    public string TextBefore { get; set; } = string.Empty;
    public string TextAfter { get; set; } = string.Empty;
    public HighlightColor Color { get; set; } = HighlightColor.Yellow;
    public string? Note { get; set; }
}
