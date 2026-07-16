using System.IO;
using System.Text.RegularExpressions;

namespace pptPortal.Helpers;

/// <summary>
/// Helper for UNC path validation and manipulation
/// </summary>
public static class UncPathHelper
{
    private static readonly Regex UncPathRegex = new(@"^\\\\[^\\]+\\[^\\]+", RegexOptions.Compiled);

    /// <summary>
    /// Validates if a path is a valid UNC path
    /// </summary>
    public static bool IsValidUncPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        return UncPathRegex.IsMatch(path);
    }

    /// <summary>
    /// Combines UNC path with a filename
    /// </summary>
    public static string CombineUncPath(string uncPath, string fileName)
    {
        // Remove trailing backslash from UNC path if present
        uncPath = uncPath.TrimEnd('\\');

        return $"{uncPath}\\{fileName}";
    }

    /// <summary>
    /// Checks if a UNC path is reachable (requires appropriate credentials)
    /// </summary>
    public static bool IsUncPathReachable(string uncPath)
    {
        try
        {
            // Extract just the share path (\\computer\share)
            var match = UncPathRegex.Match(uncPath);
            if (!match.Success)
                return false;

            var sharePath = match.Value;

            // Try to access the root of the share
            return Directory.Exists(sharePath);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Ensures parent directories exist in the UNC path
    /// </summary>
    public static void EnsureDirectoryExists(string uncPath)
    {
        var directory = Path.GetDirectoryName(uncPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <summary>
    /// Extracts the computer identifier from a UNC path
    /// </summary>
    public static string? GetComputerFromUncPath(string uncPath)
    {
        if (!IsValidUncPath(uncPath))
            return null;

        // UNC format: \\computer\share\path
        var parts = uncPath.TrimStart('\\').Split('\\');
        return parts.Length > 0 ? parts[0] : null;
    }
}
