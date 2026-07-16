using System.Diagnostics;
using System.Security.Principal;

namespace pptPortal.Services;

/// <summary>
/// Handles checking and requesting administrator elevation
/// </summary>
public class AdminElevationService
{
    /// <summary>
    /// Checks if the current process is running with administrator privileges
    /// </summary>
    public bool IsRunningAsAdmin()
    {
        try
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Restarts the application with administrator privileges
    /// </summary>
    /// <returns>True if restart was initiated, false if user cancelled UAC prompt</returns>
    public bool RestartAsAdmin()
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName,
                UseShellExecute = true,
                Verb = "runas" // Request elevation
            };

            Process.Start(processInfo);
            return true;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // User cancelled UAC prompt
            return false;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to restart as administrator: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets a user-friendly message about admin requirements
    /// </summary>
    public string GetAdminRequiredMessage()
    {
        return "Administrator privileges are required to modify configuration settings.\n\n" +
               "The application will restart with elevated permissions.\n" +
               "Click OK to continue, or Cancel to return.";
    }
}
