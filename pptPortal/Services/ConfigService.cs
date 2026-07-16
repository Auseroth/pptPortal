using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using pptPortal.Models;

namespace pptPortal.Services;

/// <summary>
/// Handles reading and writing configuration to ProgramData
/// </summary>
public class ConfigService
{
    private static readonly string ConfigDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "pptPortal"
    );

    private static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Checks if configuration file exists
    /// </summary>
    public bool ConfigExists() => File.Exists(ConfigFilePath);

    /// <summary>
    /// Loads configuration from ProgramData
    /// </summary>
    public AppConfig LoadConfig()
    {
        if (!File.Exists(ConfigFilePath))
        {
            throw new FileNotFoundException("Configuration file not found. Please run initial setup.", ConfigFilePath);
        }

        try
        {
            var json = File.ReadAllText(ConfigFilePath);
            return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load configuration: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Saves configuration to ProgramData (requires admin privileges)
    /// </summary>
    public void SaveConfig(AppConfig config)
    {
        try
        {
            // Ensure directory exists
            if (!Directory.Exists(ConfigDirectory))
            {
                Directory.CreateDirectory(ConfigDirectory);
                SetDirectoryPermissions(ConfigDirectory);
            }

            // Serialize and save
            var json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(ConfigFilePath, json);

            // Set file permissions on first creation
            if (!File.Exists(ConfigFilePath))
            {
                SetFilePermissions(ConfigFilePath);
            }
        }
        catch (UnauthorizedAccessException)
        {
            throw new UnauthorizedAccessException(
                "Administrator privileges required to save configuration. Please restart the application as administrator.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save configuration: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Sets directory permissions to require admin for write access
    /// </summary>
    private void SetDirectoryPermissions(string path)
    {
        try
        {
            var dirInfo = new DirectoryInfo(path);
            var security = dirInfo.GetAccessControl();

            // Allow authenticated users to read
            security.AddAccessRule(new FileSystemAccessRule(
                new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
                FileSystemRights.Read | FileSystemRights.ReadAndExecute,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow));

            // Allow administrators full control
            security.AddAccessRule(new FileSystemAccessRule(
                new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow));

            dirInfo.SetAccessControl(security);
        }
        catch
        {
            // If we can't set permissions, continue anyway
            // This might happen on systems with restricted security policies
        }
    }

    /// <summary>
    /// Sets file permissions to require admin for write access
    /// </summary>
    private void SetFilePermissions(string path)
    {
        try
        {
            var fileInfo = new FileInfo(path);
            var security = fileInfo.GetAccessControl();

            // Allow authenticated users to read
            security.AddAccessRule(new FileSystemAccessRule(
                new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
                FileSystemRights.Read,
                AccessControlType.Allow));

            // Allow administrators full control
            security.AddAccessRule(new FileSystemAccessRule(
                new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
                FileSystemRights.FullControl,
                AccessControlType.Allow));

            fileInfo.SetAccessControl(security);
        }
        catch
        {
            // If we can't set permissions, continue anyway
        }
    }
}
