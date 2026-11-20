using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Directives;

internal static class GatewayFieldDirectiveParser
{
    public static bool Parse(IReadOnlyList<DirectiveNode> directiveNodes)
    {
        if (directiveNodes.Count == 0)
        {
            return false;
        }

        for (var i = 0; i < directiveNodes.Count; i++)
        {
            if (FusionBuiltIns.IsGatewayFieldDirective(directiveNodes[i].Name.Value))
            {
                return true;
            }
        }

        return false;
    }
}
