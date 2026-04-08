using System.Text.RegularExpressions;

namespace Mocha.Analyzers.Utils;

/// <summary>
/// Provides helper methods for deriving a module name from the assembly name.
/// This ensures generated types are unique per assembly, avoiding collisions
/// when multiple assemblies use the Mocha mediator source generator.
/// </summary>
internal static class ModuleNameHelper
{
    private static readonly Regex s_invalidCharsRegex = new("[^a-zA-Z0-9]", RegexOptions.Compiled);

    /// <summary>
    /// Creates a module name from the assembly name by extracting the last segment
    /// (after the last dot), sanitizing invalid characters, and appending a suffix.
    /// For example, "Demo.Billing" becomes "Billing".
    /// </summary>
    public static string CreateModuleName(string? assemblyName)
    {
        if (assemblyName is null)
        {
            return "Assembly";
        }

        var lastSegment = assemblyName.Split('.').Last();
        return SanitizeIdentifier(lastSegment);
    }

    internal static string SanitizeIdentifier(string input)
    {
        var sanitized = s_invalidCharsRegex.Replace(input, "_");

        if (sanitized.Length == 0 || !char.IsLetter(sanitized[0]))
        {
            sanitized = "_" + sanitized;
        }

        return sanitized;
    }
}
