using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers;

internal static class DirectiveValueHelper
{
    public static string? GetStringArgument(
        this IReadOnlyDirectiveCollection directives,
        string directiveName,
        string argumentName)
    {
        var directive = directives.FirstOrDefault(directiveName);

        if (directive is not null
            && directive.Arguments.TryGetValue(argumentName, out var value)
            && value is StringValueNode stringValue)
        {
            return stringValue.Value;
        }

        return null;
    }
}
