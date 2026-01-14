using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phoenix.Core.Interfaces;
using Phoenix.Core.Models;
using Phoenix.UI.WPF.Services;

namespace Phoenix.UI.WPF.ViewModels;

/// <summary>
/// Main window view model.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ILibraryService _libraryService;
    private readonly ISettingsService _settingsService;
    private readonly IEpubParser _epubParser;

    [ObservableProperty]
    private ObservableObject? _currentView;

    [ObservableProperty]
    private Book? _currentBook;

    [ObservableProperty]
    private UserSettings? _settings;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isSidebarVisible = true;

    [ObservableProperty]
    private bool _isReading;

    public LibraryViewModel LibraryViewModel { get; }
    public ReaderViewModel ReaderViewModel { get; }
    public SettingsViewModel SettingsViewModel { get; }

    public MainViewModel(
        ILibraryService libraryService,
        ISettingsService settingsService,
        IEpubParser epubParser,
        IBookmarkService bookmarkService,
        IReadingProgressService progressService)
    {
        _libraryService = libraryService;
        _settingsService = settingsService;
        _epubParser = epubParser;

        LibraryViewModel = new LibraryViewModel(libraryService, this);
        ReaderViewModel = new ReaderViewModel(epubParser, bookmarkService, progressService, this);
        SettingsViewModel = new SettingsViewModel(settingsService, this);

        CurrentView = LibraryViewModel;
    }

    /// <summary>
    /// Updates sidebar visibility based on reading state.
    /// Sidebar should only be visible when reading AND user hasn't closed it.
    /// </summary>
    partial void OnIsReadingChanged(bool value)
    {
        if (!value)
        {
            // Hide sidebar when not reading
            IsSidebarVisible = false;
        }
        else if (Settings?.SidebarVisible ?? true)
        {
            // Show sidebar when entering reader if it was enabled in settings
            IsSidebarVisible = true;
        }
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading settings...";

        try
        {
            Settings = await _settingsService.GetSettingsAsync();
            IsSidebarVisible = Settings.SidebarVisible;
            
            // Apply the saved theme to the application UI
            ThemeManager.SetTheme(Settings.Theme);
            
            await LibraryViewModel.LoadBooksAsync();
            StatusMessage = "Ready";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ShowLibrary()
    {
        IsReading = false;
        CurrentView = LibraryViewModel;
    }

    [RelayCommand]
    private void ShowSettings()
    {
        // Settings is now a popup, but keep this for compatibility
        IsReading = false;
        CurrentView = LibraryViewModel;
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarVisible = !IsSidebarVisible;
    }

    /// <summary>
    /// Called when a setting changes to refresh the reader content.
    /// </summary>
    public void OnSettingsChanged()
    {
        // Save settings asynchronously
        if (Settings != null)
        {
            _ = _settingsService.SaveSettingsAsync(Settings);
            
            // Apply the theme to the application UI
            ThemeManager.SetTheme(Settings.Theme);
        }
        
        // Refresh reader content if currently reading
        if (IsReading)
        {
            ReaderViewModel.RefreshContent();
        }
    }

    [RelayCommand]
    private async Task OpenBookAsync(Book book)
    {
        CurrentBook = book;
        CurrentView = ReaderViewModel;
        IsReading = true;
        await ReaderViewModel.OpenBookAsync(book);
        
        // Update last opened
        book.LastOpened = DateTime.UtcNow;
        await _libraryService.UpdateBookAsync(book);
    }

    [RelayCommand]
    private async Task OpenFileAsync(string filePath)
    {
        IsLoading = true;
        StatusMessage = "Opening book...";

        try
        {
            var book = await _libraryService.ImportBookAsync(filePath);
            await OpenBookAsync(book);
            await LibraryViewModel.LoadBooksAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error opening file: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
