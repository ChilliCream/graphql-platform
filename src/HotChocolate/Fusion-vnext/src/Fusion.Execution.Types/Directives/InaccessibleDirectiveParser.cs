using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Directives;

internal static class InaccessibleDirectiveParser
{
    public static bool Parse(IReadOnlyList<DirectiveNode> directiveNodes)
    {
        if (directiveNodes.Count == 0)
        {
            return false;
        }

        for (var i = 0; i < directiveNodes.Count; i++)
        {
            if (directiveNodes[i].Name.Value.Equals(FusionBuiltIns.Inaccessible))
            {
                return true;
            }
        }

        return false;
    }
}
