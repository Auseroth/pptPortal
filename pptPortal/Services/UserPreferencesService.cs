using System.IO;
using System.Text.Json;
using pptPortal.Models;

namespace pptPortal.Services;

/// <summary>
/// Handles user-specific preferences (non-admin, per-user storage)
/// </summary>
public class UserPreferencesService
{
    private static readonly string UserConfigDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "pptPortal"
    );

    private static readonly string PreferencesFilePath = Path.Combine(UserConfigDirectory, "user-preferences.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public UserPreferences LoadPreferences()
    {
        if (!File.Exists(PreferencesFilePath))
        {
            return new UserPreferences();
        }

        try
        {
            var json = File.ReadAllText(PreferencesFilePath);
            return JsonSerializer.Deserialize<UserPreferences>(json, JsonOptions) ?? new UserPreferences();
        }
        catch
        {
            return new UserPreferences();
        }
    }

    public void SavePreferences(UserPreferences preferences)
    {
        try
        {
            // Ensure directory exists (no admin required for AppData)
            if (!Directory.Exists(UserConfigDirectory))
            {
                Directory.CreateDirectory(UserConfigDirectory);
            }

            var json = JsonSerializer.Serialize(preferences, JsonOptions);
            File.WriteAllText(PreferencesFilePath, json);
        }
        catch (Exception ex)
        {
            // Log or silently fail - preferences are not critical
            Console.WriteLine($"Failed to save preferences: {ex.Message}");
        }
    }
}