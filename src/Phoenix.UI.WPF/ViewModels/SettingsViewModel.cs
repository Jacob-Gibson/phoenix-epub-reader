using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phoenix.Core.Interfaces;
using Phoenix.Core.Models;

namespace Phoenix.UI.WPF.ViewModels;

/// <summary>
/// View model for the settings view.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly MainViewModel _mainViewModel;

    [ObservableProperty]
    private UserSettings? _settings;

    [ObservableProperty]
    private bool _hasChanges;

    public List<string> AvailableFonts { get; } =
    [
        "Segoe UI",
        "Georgia",
        "Times New Roman",
        "Arial",
        "Verdana",
        "Calibri",
        "Cambria",
        "Palatino Linotype",
        "Book Antiqua",
        "Garamond"
    ];

    public List<int> AvailableFontSizes { get; } =
    [
        12, 14, 16, 18, 20, 22, 24, 28, 32
    ];

    public List<double> AvailableLineHeights { get; } =
    [
        1.2, 1.4, 1.6, 1.8, 2.0
    ];

    public List<int> AvailableMargins { get; } =
    [
        20, 40, 60, 80, 100
    ];

    public SettingsViewModel(ISettingsService settingsService, MainViewModel mainViewModel)
    {
        _settingsService = settingsService;
        _mainViewModel = mainViewModel;
    }

    public async Task LoadSettingsAsync()
    {
        Settings = await _settingsService.GetSettingsAsync();
        HasChanges = false;
    }

    [RelayCommand]
    private void SetTheme(ReaderTheme theme)
    {
        if (Settings != null)
        {
            Settings.Theme = theme;
            HasChanges = true;
            OnPropertyChanged(nameof(Settings));
        }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        if (Settings != null)
        {
            await _settingsService.SaveSettingsAsync(Settings);
            _mainViewModel.Settings = Settings;
            HasChanges = false;
        }
    }

    [RelayCommand]
    private async Task ResetSettingsAsync()
    {
        Settings = await _settingsService.ResetSettingsAsync();
        _mainViewModel.Settings = Settings;
        HasChanges = false;
    }

    [RelayCommand]
    private void GoBack()
    {
        _mainViewModel.ShowLibraryCommand.Execute(null);
    }

    partial void OnSettingsChanged(UserSettings? value)
    {
        HasChanges = true;
    }
}
