using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using pptPortal.Models;

namespace pptPortal.Services;

/// <summary>
/// Handles credential storage and retrieval using DPAPI or Windows Credential Manager
/// </summary>
public class CredentialService
{
    /// <summary>
    /// Saves a credential using the specified storage mode
    /// </summary>
    public void SaveCredential(TargetProfile profile, string password, CredentialStorageMode mode)
    {
        switch (mode)
        {
            case CredentialStorageMode.DPAPI:
                profile.EncryptedPassword = EncryptWithDPAPI(password);
                break;

            case CredentialStorageMode.CredentialManager:
                SaveToCredentialManager(profile.CredentialId, profile.AdminUsername, password);
                profile.EncryptedPassword = null; // Not stored in config
                break;

            default:
                throw new ArgumentException($"Unsupported credential storage mode: {mode}");
        }
    }

    /// <summary>
    /// Retrieves a credential using the specified storage mode
    /// </summary>
    public string? RetrieveCredential(TargetProfile profile, CredentialStorageMode mode)
    {
        switch (mode)
        {
            case CredentialStorageMode.DPAPI:
                return profile.EncryptedPassword != null
                    ? DecryptWithDPAPI(profile.EncryptedPassword)
                    : null;

            case CredentialStorageMode.CredentialManager:
                return RetrieveFromCredentialManager(profile.CredentialId);

            default:
                throw new ArgumentException($"Unsupported credential storage mode: {mode}");
        }
    }

    /// <summary>
    /// Deletes a credential from the specified storage mode
    /// </summary>
    public void DeleteCredential(TargetProfile profile, CredentialStorageMode mode)
    {
        if (mode == CredentialStorageMode.CredentialManager)
        {
            DeleteFromCredentialManager(profile.CredentialId);
        }
        // DPAPI credentials are stored in config, deleted when profile is removed
    }

    #region DPAPI Methods

    private string EncryptWithDPAPI(string plainText)
    {
        try
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = ProtectedData.Protect(
                plainBytes,
                null, // Optional entropy
                DataProtectionScope.LocalMachine // Machine-wide scope
            );
            return Convert.ToBase64String(encryptedBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to encrypt credential with DPAPI: {ex.Message}", ex);
        }
    }

    private string DecryptWithDPAPI(string encryptedText)
    {
        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedText);
            var plainBytes = ProtectedData.Unprotect(
                encryptedBytes,
                null, // Optional entropy
                DataProtectionScope.LocalMachine
            );
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to decrypt credential with DPAPI: {ex.Message}", ex);
        }
    }

    #endregion

    #region Credential Manager Methods

    private void SaveToCredentialManager(string target, string username, string password)
    {
        try
        {
            var credential = new CREDENTIAL
            {
                Type = CRED_TYPE.GENERIC,
                TargetName = target,
                UserName = username,
                CredentialBlob = Marshal.StringToCoTaskMemUni(password),
                CredentialBlobSize = (uint)(password.Length * 2), // Unicode = 2 bytes per char
                Persist = CRED_PERSIST.LOCAL_MACHINE,
                AttributeCount = 0,
                Attributes = IntPtr.Zero,
                Comment = "pptPortal Profile Credential"
            };

            if (!CredWrite(ref credential, 0))
            {
                throw new InvalidOperationException($"Failed to write credential to Credential Manager. Error: {Marshal.GetLastWin32Error()}");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save credential to Credential Manager: {ex.Message}", ex);
        }
    }

    private string? RetrieveFromCredentialManager(string target)
    {
        try
        {
            if (CredRead(target, CRED_TYPE.GENERIC, 0, out var credPtr))
            {
                var credential = Marshal.PtrToStructure<CREDENTIAL>(credPtr);
                var password = Marshal.PtrToStringUni(credential.CredentialBlob, (int)credential.CredentialBlobSize / 2);
                CredFree(credPtr);
                return password;
            }

            return null; // Credential not found
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to retrieve credential from Credential Manager: {ex.Message}", ex);
        }
    }

    private void DeleteFromCredentialManager(string target)
    {
        try
        {
            CredDelete(target, CRED_TYPE.GENERIC, 0);
        }
        catch
        {
            // Ignore errors if credential doesn't exist
        }
    }

    #endregion

    #region Windows Credential Manager P/Invoke

    [DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredWrite([In] ref CREDENTIAL userCredential, [In] uint flags);

    [DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredRead(string target, CRED_TYPE type, int reservedFlag, out IntPtr credentialPtr);

    [DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredDelete(string target, CRED_TYPE type, int flags);

    [DllImport("Advapi32.dll")]
    private static extern void CredFree([In] IntPtr cred);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public uint Flags;
        public CRED_TYPE Type;
        public string TargetName;
        public string Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public CRED_PERSIST Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string TargetAlias;
        public string UserName;
    }

    private enum CRED_TYPE : uint
    {
        GENERIC = 1,
        DOMAIN_PASSWORD = 2,
        DOMAIN_CERTIFICATE = 3,
        DOMAIN_VISIBLE_PASSWORD = 4,
        GENERIC_CERTIFICATE = 5,
        DOMAIN_EXTENDED = 6,
        MAXIMUM = 7,
        MAXIMUM_EX = 1007
    }

    private enum CRED_PERSIST : uint
    {
        SESSION = 1,
        LOCAL_MACHINE = 2,
        ENTERPRISE = 3
    }

    #endregion
}
