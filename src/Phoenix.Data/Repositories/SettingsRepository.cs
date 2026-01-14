using System.Linq;
using System.Threading.Tasks;
using Phoenix.Core.Interfaces;
using Phoenix.Core.Models;

namespace Phoenix.Data.Repositories;

/// <summary>
/// LiteDB implementation of the settings service.
/// </summary>
public class SettingsRepository : ISettingsService
{
    private readonly PhoenixDatabase _database;

    public SettingsRepository(PhoenixDatabase database)
    {
        _database = database;
    }

    /// <inheritdoc />
    public Task<UserSettings> GetSettingsAsync()
    {
        var settings = _database.Settings.FindById(1);
        
        if (settings == null)
        {
            settings = new UserSettings();
            _database.Settings.Insert(settings);
        }
        
        return Task.FromResult(settings);
    }

    /// <inheritdoc />
    public Task<bool> SaveSettingsAsync(UserSettings settings)
    {
        settings.Id = 1; // Ensure singleton ID
        
        var existing = _database.Settings.FindById(1);
        
        if (existing != null)
        {
            _database.Settings.Update(settings);
        }
        else
        {
            _database.Settings.Insert(settings);
        }
        
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<UserSettings> ResetSettingsAsync()
    {
        var settings = new UserSettings();
        _database.Settings.DeleteAll();
        _database.Settings.Insert(settings);
        return Task.FromResult(settings);
    }
}
