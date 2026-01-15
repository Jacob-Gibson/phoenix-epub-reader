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

    /// <summary>
    /// Gets the settings from the main view model (shared instance).
    /// </summary>
    public UserSettings? Settings => _mainViewModel.Settings;

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

    /// <summary>
    /// Refreshes the Settings property binding.
    /// </summary>
    public void RefreshSettings()
    {
        OnPropertyChanged(nameof(Settings));
    }

    /// <summary>
    /// Saves settings and notifies the main view model.
    /// </summary>
    public async Task SaveAndApplyAsync()
    {
        if (Settings != null)
        {
            await _settingsService.SaveSettingsAsync(Settings);
            _mainViewModel.OnSettingsChanged();
        }
    }

    [RelayCommand]
    private async Task ResetSettingsAsync()
    {
        var defaultSettings = await _settingsService.ResetSettingsAsync();
        _mainViewModel.Settings = defaultSettings;
        OnPropertyChanged(nameof(Settings));
        _mainViewModel.OnSettingsChanged();
    }

    [RelayCommand]
    private void GoBack()
    {
        _mainViewModel.ShowLibraryCommand.Execute(null);
    }
}
