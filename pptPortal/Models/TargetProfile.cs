namespace pptPortal.Models;

/// <summary>
/// Represents a target computer profile for file uploads
/// </summary>
public class TargetProfile
{
    /// <summary>
    /// User-friendly name for this profile
    /// </summary>
    public string ProfileName { get; set; } = string.Empty;

    /// <summary>
    /// Computer identifier - can be IP address, hostname, or computer name
    /// </summary>
    public string ComputerIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Full UNC path to the target folder (e.g., \\192.168.1.100\KioskShare\Presentations)
    /// </summary>
    public string TargetUncPath { get; set; } = string.Empty;

    /// <summary>
    /// Fixed filename to use when saving on target (e.g., Presentation.pptx)
    /// </summary>
    public string TargetFileName { get; set; } = string.Empty;

    /// <summary>
    /// Admin username in format: DOMAIN\User or COMPUTER\User (e.g., 192.168.1.100\Administrator)
    /// </summary>
    public string AdminUsername { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted password (DPAPI) or Credential Manager reference
    /// </summary>
    public string? EncryptedPassword { get; set; }

    /// <summary>
    /// Unique identifier for Credential Manager storage
    /// </summary>
    public string CredentialId => $"pptPortal_{ProfileName}_{ComputerIdentifier}";
}
