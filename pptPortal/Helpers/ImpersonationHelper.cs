using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace pptPortal.Helpers;

/// <summary>
/// Helper for Windows impersonation to access network resources with alternate credentials
/// </summary>
public class ImpersonationHelper : IDisposable
{
    private SafeAccessTokenHandle? _tokenHandle;
    private bool _disposed;

    /// <summary>
    /// Begins impersonation with the specified credentials
    /// </summary>
    public void Impersonate(string username, string domain, string password)
    {
        // Parse username if it contains domain (e.g., "DOMAIN\User" or "192.168.1.100\User")
        if (username.Contains('\\'))
        {
            var parts = username.Split('\\');
            domain = parts[0];
            username = parts[1];
        }

        // LOGON32_LOGON_NEW_CREDENTIALS = 9: For network access only
        const int LOGON32_LOGON_NEW_CREDENTIALS = 9;
        const int LOGON32_PROVIDER_DEFAULT = 0;

        IntPtr token = IntPtr.Zero;
        bool success = LogonUser(
            username,
            domain,
            password,
            LOGON32_LOGON_NEW_CREDENTIALS,
            LOGON32_PROVIDER_DEFAULT,
            out token
        );

        if (!success || token == IntPtr.Zero)
        {
            int errorCode = Marshal.GetLastWin32Error();
            throw new InvalidOperationException(
                $"Failed to impersonate user. Error code: {errorCode}. " +
                $"Check username format (DOMAIN\\User or COMPUTER\\User) and password.");
        }

        _tokenHandle = new SafeAccessTokenHandle(token);
    }

    /// <summary>
    /// Executes an action under impersonation
    /// </summary>
    public void ExecuteImpersonated(Action action)
    {
        if (_tokenHandle == null || _tokenHandle.IsInvalid)
        {
            throw new InvalidOperationException("Must call Impersonate() first.");
        }

        WindowsIdentity.RunImpersonated(_tokenHandle, action);
    }

    /// <summary>
    /// Executes a function under impersonation and returns the result
    /// </summary>
    public T ExecuteImpersonated<T>(Func<T> func)
    {
        if (_tokenHandle == null || _tokenHandle.IsInvalid)
        {
            throw new InvalidOperationException("Must call Impersonate() first.");
        }

        return WindowsIdentity.RunImpersonated(_tokenHandle, func);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _tokenHandle?.Dispose();
        _disposed = true;
    }

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool LogonUser(
        string lpszUsername,
        string lpszDomain,
        string lpszPassword,
        int dwLogonType,
        int dwLogonProvider,
        out IntPtr phToken
    );
}