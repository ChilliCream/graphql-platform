using System.Text.RegularExpressions;

namespace HotChocolate.Fusion.Packaging;

/// <summary>
/// Validates schema names according to the Fusion Execution Schema Format specification.
/// </summary>
internal static partial class SchemaNameValidator
{
    // Schema Name Grammar:
    // Name ::= NameStart NameContinue* [lookahead != NameContinue]
    // NameStart ::= Letter | `_`
    // NameContinue ::= Letter | Digit | `_` | `-`

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_-]*$")]
    private static partial Regex SchemaNameRegex();

    /// <summary>
    /// Validates whether the given string is a valid schema name.
    /// </summary>
    /// <param name="name">The schema name to validate.</param>
    /// <returns>True if the name is valid, false otherwise.</returns>
    public static bool IsValidSchemaName(string? name)
    {
        return !string.IsNullOrEmpty(name) && SchemaNameRegex().IsMatch(name) && name.Any(char.IsLetterOrDigit);
    }
}
