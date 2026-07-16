namespace pptPortal.Models;

/// <summary>
/// Defines the method used to store credentials
/// </summary>
public enum CredentialStorageMode
{
    /// <summary>
    /// Use Windows Data Protection API (DPAPI) - Default
    /// </summary>
    DPAPI,

    /// <summary>
    /// Use Windows Credential Manager
    /// </summary>
    CredentialManager
}
