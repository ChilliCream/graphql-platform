using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Directives;

internal static class DeprecatedDirectiveParser
{
    public static bool CanParse(DirectiveNode directiveNode)
        => directiveNode.Name.Value == "deprecated";

    public static DeprecatedDirective Parse(DirectiveNode directiveNode)
    {
        var reason = DirectiveNames.Deprecated.Arguments.DefaultReason;

        foreach (var argument in directiveNode.Arguments)
        {
            switch (argument.Name.Value)
            {
                case "reason":
                    if (argument.Value is StringValueNode reasonValue
                        && !string.IsNullOrWhiteSpace(reasonValue.Value))
                    {
                        reason = reasonValue.Value;
                    }

                    break;

                default:
                    throw new DirectiveParserException(
                        $"The argument `{argument.Name.Value}` is not supported on @deprecated.");
            }
        }

        return new DeprecatedDirective(reason);
    }

    public static string? ParseReason(IReadOnlyList<DirectiveNode> directiveNodes)
    {
        for (var i = 0; i < directiveNodes.Count; i++)
        {
            var directiveNode = directiveNodes[i];

            if (CanParse(directiveNode))
            {
                return Parse(directiveNode).Reason;
            }
        }

        return null;
    }
}
