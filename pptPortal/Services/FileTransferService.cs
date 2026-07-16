using System.IO;
using pptPortal.Helpers;
using pptPortal.Models;

namespace pptPortal.Services;

/// <summary>
/// Handles file transfer to remote computers via UNC paths
/// </summary>
public class FileTransferService
{
    private readonly CredentialService _credentialService;

    public FileTransferService(CredentialService credentialService)
    {
        _credentialService = credentialService;
    }

    /// <summary>
    /// Uploads a file to the target profile location
    /// </summary>
    public FileTransferResult UploadFile(string sourceFilePath, TargetProfile profile, CredentialStorageMode credentialMode)
    {
        var result = new FileTransferResult();

        try
        {
            // Validate source file
            if (!File.Exists(sourceFilePath))
            {
                result.Success = false;
                result.ErrorMessage = "Source file does not exist.";
                return result;
            }

            // Validate UNC path
            if (!UncPathHelper.IsValidUncPath(profile.TargetUncPath))
            {
                result.Success = false;
                result.ErrorMessage = $"Invalid UNC path: {profile.TargetUncPath}";
                return result;
            }

            // Retrieve credentials
            var password = _credentialService.RetrieveCredential(profile, credentialMode);
            if (string.IsNullOrEmpty(password))
            {
                result.Success = false;
                result.ErrorMessage = "Failed to retrieve credentials for this profile.";
                return result;
            }

            // Build target file path
            var targetFilePath = UncPathHelper.CombineUncPath(profile.TargetUncPath, profile.TargetFileName);

            // Read source file into memory BEFORE impersonation
            byte[] fileBytes = File.ReadAllBytes(sourceFilePath);
            var fileName = Path.GetFileName(sourceFilePath);

            // Now impersonate to write to target
            using (var impersonation = new ImpersonationHelper())
            {
                var domain = string.Empty;
                var username = profile.AdminUsername;

                impersonation.Impersonate(username, domain, password);

                // Execute file operations under impersonation
                impersonation.ExecuteImpersonated(() =>
                {
                    // Check if share is reachable
                    if (!UncPathHelper.IsUncPathReachable(profile.TargetUncPath))
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Cannot reach target: {profile.TargetUncPath}. " +
                                            "Check network connectivity, share permissions, and credentials.";
                        return;
                    }

                    // Ensure target directory exists
                    UncPathHelper.EnsureDirectoryExists(targetFilePath);

                    // Write file from memory with overwrite
                    File.WriteAllBytes(targetFilePath, fileBytes);

                    result.TargetPath = targetFilePath;
                });
            }

            // Verify file was copied successfully
            result.Success = VerifyFileExists(targetFilePath, profile, credentialMode);
            if (!result.Success)
            {
                result.ErrorMessage = "File copy completed but verification failed. File may not be accessible on target.";
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Access denied: {ex.Message}. Check credentials and share permissions.";
        }
        catch (IOException ex)
        {
            result.Success = false;
            result.ErrorMessage = $"File transfer failed: {ex.Message}";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Unexpected error: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// Verifies that the file exists and is accessible on the target
    /// </summary>
    private bool VerifyFileExists(string targetFilePath, TargetProfile profile, CredentialStorageMode credentialMode)
    {
        try
        {
            var password = _credentialService.RetrieveCredential(profile, credentialMode);
            if (string.IsNullOrEmpty(password))
                return false;

            using var impersonation = new ImpersonationHelper();
            impersonation.Impersonate(profile.AdminUsername, string.Empty, password);

            return impersonation.ExecuteImpersonated(() =>
            {
                // Check if file exists and is readable
                if (!File.Exists(targetFilePath))
                    return false;

                // Try to open the file to confirm it's accessible
                using var stream = File.OpenRead(targetFilePath);
                return true;
            });
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Result of a file transfer operation
/// </summary>
public class FileTransferResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? TargetPath { get; set; }
}
