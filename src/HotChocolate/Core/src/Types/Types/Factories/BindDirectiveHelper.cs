using HotChocolate.Language;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Factories;

internal static class BindDirectiveHelper
{
    public const string Name = "bind";
    public const string ToArgument = "to";

    public static bool IsBindingDirective(this DirectiveNode directiveNode)
        => string.Equals(directiveNode.Name.Value, Name, StringComparison.Ordinal);

    public static string? GetBindingValue(this Language.IHasDirectives syntaxNode)
    {
        var directive = syntaxNode.Directives.FirstOrDefault(
            t => t.Name.Value == Name);

        if (directive is null)
        {
            return null;
        }

        if (directive.Arguments.Count == 1)
        {
            var to = directive.Arguments[0];

            if (to.Name.Value.EqualsOrdinal(ToArgument) &&
                to.Value is StringValueNode { Value: { Length: > 0, } value, })
            {
                return value;
            }
        }

        return null;
    }
}
