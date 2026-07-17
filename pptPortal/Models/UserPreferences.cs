namespace pptPortal.Models;

/// <summary>
/// User-specific preferences stored in AppData
/// </summary>
public class UserPreferences
{
    /// <summary>
    /// Last browsed file path
    /// </summary>
    public string? LastFilePath { get; set; }

    /// <summary>
    /// Last selected profile name (helps sync across sessions)
    /// </summary>
    public string? LastSelectedProfile { get; set; }

    /// <summary>
    /// Index of the currently selected profile (user preference, not system config)
    /// </summary>
    public int? SelectedProfileIndex { get; set; }

    /// <summary>
    /// Recently used file paths (max 10)
    /// </summary>
    public List<string> RecentFiles { get; set; } = new();

    /// <summary>
    /// Per-computer specific settings (uses Environment.MachineName as key)
    /// </summary>
    public Dictionary<string, ComputerPreferences> ComputerSpecificSettings { get; set; } = new();
}

/// <summary>
/// Computer-specific preferences for multi-machine scenarios
/// </summary>
public class ComputerPreferences
{
    public string? LastFilePath { get; set; }
    public string? PreferredProfile { get; set; }
    public string? DefaultDirectory { get; set; }
}