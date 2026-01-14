using System.Threading.Tasks;
using Phoenix.Core.Models;

namespace Phoenix.Core.Interfaces;

/// <summary>
/// Interface for managing user settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the current user settings.
    /// </summary>
    Task<UserSettings> GetSettingsAsync();

    /// <summary>
    /// Saves user settings.
    /// </summary>
    Task<bool> SaveSettingsAsync(UserSettings settings);

    /// <summary>
    /// Resets settings to defaults.
    /// </summary>
    Task<UserSettings> ResetSettingsAsync();
}
