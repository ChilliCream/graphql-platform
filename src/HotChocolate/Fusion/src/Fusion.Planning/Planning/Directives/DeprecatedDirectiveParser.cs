using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Directives;

internal static class DeprecatedDirectiveParser
{
    public static bool CanParse(DirectiveNode directiveNode)
        => directiveNode.Name.Value == "deprecated";

    public static Deprecated Parse(DirectiveNode directiveNode)
    {
        var reason = "No longer supported";

        foreach (var argument in directiveNode.Arguments)
        {
            switch (argument.Name.Value)
            {
                case "reason":
                    reason = ((StringValueNode)argument.Value).Value;
                    break;

                default:
                    throw new DirectiveParserException(
                        $"The argument `{argument.Name.Value}` is not supported on @deprecated.");
            }
        }

        return new Deprecated(reason);
    }

    public static bool TryParse(
        IReadOnlyList<DirectiveNode> directiveNodes,
        [NotNullWhen(true)] out Deprecated? deprecated)
    {
        for (var i = 0; i < directiveNodes.Count; i++)
        {
            var directiveNode = directiveNodes[i];
            if (CanParse(directiveNode))
            {
                deprecated = Parse(directiveNode);
                return true;
            }
        }

        deprecated = null;
        return false;
    }
}
