namespace pptPortal.Models;

/// <summary>
/// Root configuration for the application
/// </summary>
public class AppConfig
{
    /// <summary>
    /// Credential storage method
    /// </summary>
    public CredentialStorageMode CredentialMode { get; set; } = CredentialStorageMode.DPAPI;

    /// <summary>
    /// List of target computer profiles
    /// </summary>
    public List<TargetProfile> Profiles { get; set; } = new();

    /// <summary>
    /// Index of the currently selected profile
    /// </summary>
    public int SelectedProfileIndex { get; set; } = 0;

    /// <summary>
    /// Gets the currently selected profile, or null if none
    /// </summary>
    public TargetProfile? SelectedProfile =>
        Profiles.Count > 0 && SelectedProfileIndex >= 0 && SelectedProfileIndex < Profiles.Count
            ? Profiles[SelectedProfileIndex]
            : null;
}
