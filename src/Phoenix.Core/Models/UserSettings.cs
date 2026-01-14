namespace Phoenix.Core.Models;

/// <summary>
/// User settings for the ePub reader.
/// </summary>
public class UserSettings
{
    /// <summary>
    /// Singleton ID (always 1 for user settings).
    /// </summary>
    public int Id { get; set; } = 1;

    /// <summary>
    /// The font family for reading.
    /// </summary>
    public string FontFamily { get; set; } = "Segoe UI";

    /// <summary>
    /// The font size in pixels.
    /// </summary>
    public int FontSize { get; set; } = 16;

    /// <summary>
    /// The line height multiplier.
    /// </summary>
    public double LineHeight { get; set; } = 1.6;

    /// <summary>
    /// The margin size in pixels.
    /// </summary>
    public int MarginSize { get; set; } = 40;

    /// <summary>
    /// The current theme.
    /// </summary>
    public ReaderTheme Theme { get; set; } = ReaderTheme.Light;

    /// <summary>
    /// The path to the last opened book.
    /// </summary>
    public string? LastOpenedBookPath { get; set; }

    /// <summary>
    /// Whether to remember the last reading position.
    /// </summary>
    public bool RememberPosition { get; set; } = true;

    /// <summary>
    /// The width of the sidebar in pixels.
    /// </summary>
    public int SidebarWidth { get; set; } = 280;

    /// <summary>
    /// Whether the sidebar is visible.
    /// </summary>
    public bool SidebarVisible { get; set; } = true;

    /// <summary>
    /// The default library folder path.
    /// </summary>
    public string? LibraryPath { get; set; }
}

/// <summary>
/// Available reader themes.
/// </summary>
public enum ReaderTheme
{
    /// <summary>Use the ePub's native styling.</summary>
    Default = 0,
    /// <summary>Light theme with white background.</summary>
    Light = 1,
    /// <summary>Dark theme with dark background.</summary>
    Dark = 2,
    /// <summary>Sepia theme for reduced eye strain.</summary>
    Sepia = 3
}
