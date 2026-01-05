using System.Text.RegularExpressions;

namespace HotChocolate.Adapters.Mcp.Packaging;

/// <summary>
/// Validates names for prompts and tools in the archive.
/// </summary>
internal static partial class NameValidator
{
    [GeneratedRegex(@"^[A-Za-z0-9_.-]{1,128}\z")]
    private static partial Regex NameRegex();

    /// <summary>
    /// Validates whether the given string is a valid name.
    /// </summary>
    /// <param name="name">The name to validate.</param>
    /// <returns>True if the name is valid, false otherwise.</returns>
    public static bool IsValidName(string? name)
    {
        return !string.IsNullOrEmpty(name) && NameRegex().IsMatch(name) && name.Any(char.IsLetterOrDigit);
    }
}
