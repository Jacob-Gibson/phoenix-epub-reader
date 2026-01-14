using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phoenix.Core.Interfaces;
using Phoenix.Core.Models;

namespace Phoenix.UI.WPF.ViewModels;

/// <summary>
/// View model for the library view.
/// </summary>
public partial class LibraryViewModel : ObservableObject
{
    private readonly ILibraryService _libraryService;
    private readonly MainViewModel _mainViewModel;

    [ObservableProperty]
    private ObservableCollection<Book> _books = [];

    [ObservableProperty]
    private ObservableCollection<Book> _recentBooks = [];

    [ObservableProperty]
    private Book? _selectedBook;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private LibraryViewMode _viewMode = LibraryViewMode.Grid;

    public LibraryViewModel(ILibraryService libraryService, MainViewModel mainViewModel)
    {
        _libraryService = libraryService;
        _mainViewModel = mainViewModel;
    }

    public async Task LoadBooksAsync()
    {
        IsLoading = true;

        try
        {
            var allBooks = await _libraryService.GetAllBooksAsync();
            Books = new ObservableCollection<Book>(allBooks.OrderBy(b => b.Title));

            var recent = await _libraryService.GetRecentBooksAsync(5);
            RecentBooks = new ObservableCollection<Book>(recent);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        IsLoading = true;

        try
        {
            var results = await _libraryService.SearchBooksAsync(SearchQuery);
            Books = new ObservableCollection<Book>(results.OrderBy(b => b.Title));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task OpenBookAsync(Book? book)
    {
        if (book != null)
        {
            await _mainViewModel.OpenBookCommand.ExecuteAsync(book);
        }
    }

    [RelayCommand]
    private async Task DeleteBookAsync(Book? book)
    {
        if (book != null)
        {
            await _libraryService.DeleteBookAsync(book.Id);
            Books.Remove(book);
            RecentBooks.Remove(book);
        }
    }

    [RelayCommand]
    private void SetGridView()
    {
        ViewMode = LibraryViewMode.Grid;
    }

    [RelayCommand]
    private void SetListView()
    {
        ViewMode = LibraryViewMode.List;
    }

    partial void OnSearchQueryChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _ = LoadBooksAsync();
        }
    }
}

public enum LibraryViewMode
{
    Grid,
    List
}
