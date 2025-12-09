using System.Text.RegularExpressions;

namespace HotChocolate.Adapters.OpenApi.Packaging;

/// <summary>
/// Validates names for endpoints and models in the archive.
/// </summary>
internal static partial class NameValidator
{
    // Name Grammar:
    // Name ::= NameStart NameContinue* [lookahead != NameContinue]
    // NameStart ::= Letter | `_`
    // NameContinue ::= Letter | Digit | `_` | `-`

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_-]*$")]
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
