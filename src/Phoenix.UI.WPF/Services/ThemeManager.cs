using System;
using System.Linq;
using System.Windows;
using Phoenix.Core.Models;

namespace Phoenix.UI.WPF.Services;

/// <summary>
/// Manages application theme switching at runtime.
/// </summary>
public static class ThemeManager
{
    private static readonly string[] ThemeFileNames = ["LightTheme.xaml", "DarkTheme.xaml", "SepiaTheme.xaml"];

    /// <summary>
    /// Applies the specified theme to the application.
    /// </summary>
    public static void ApplyTheme(ReaderTheme theme)
    {
        // Default and Light both use LightTheme for the app UI
        var themeUri = theme switch
        {
            ReaderTheme.Dark => new Uri("Resources/Themes/DarkTheme.xaml", UriKind.Relative),
            ReaderTheme.Sepia => new Uri("Resources/Themes/SepiaTheme.xaml", UriKind.Relative),
            _ => new Uri("Resources/Themes/LightTheme.xaml", UriKind.Relative)
        };

        var newThemeDictionary = new ResourceDictionary { Source = themeUri };

        var mergedDicts = Application.Current.Resources.MergedDictionaries;

        // Find and remove any existing theme dictionaries
        var existingThemes = mergedDicts
            .Where(d => d.Source != null && ThemeFileNames.Any(name => d.Source.OriginalString.Contains(name)))
            .ToList();

        foreach (var oldTheme in existingThemes)
        {
            mergedDicts.Remove(oldTheme);
        }

        // Add the new theme dictionary at the beginning
        mergedDicts.Insert(0, newThemeDictionary);
    }

    /// <summary>
    /// Gets the current theme based on settings.
    /// </summary>
    public static ReaderTheme CurrentTheme { get; private set; } = ReaderTheme.Default;

    /// <summary>
    /// Sets the current theme and applies it.
    /// </summary>
    public static void SetTheme(ReaderTheme theme)
    {
        CurrentTheme = theme;
        ApplyTheme(theme);
    }
}
