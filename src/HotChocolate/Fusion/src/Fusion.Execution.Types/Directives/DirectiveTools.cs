using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Directives;

internal static class DirectiveTools
{
    public static IImmutableList<DirectiveNode> GetUserDirectives(
        IReadOnlyList<DirectiveNode> directiveNodes,
        bool applySerializeAsToScalars)
    {
        if (directiveNodes.Count == 0)
        {
            return ImmutableArray<DirectiveNode>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<DirectiveNode>();

        foreach (var directiveNode in directiveNodes)
        {
            if (RequiredDirectiveParser.CanParse(directiveNode)
                || FieldDirectiveParser.CanParse(directiveNode)
                || TypeDirectiveParser.CanParse(directiveNode)
                || LookupDirectiveParser.CanParse(directiveNode))
            {
                continue;
            }

            if (DeprecatedDirectiveParser.CanParse(directiveNode))
            {
                continue;
            }

            if (FusionBuiltIns.IsBuiltInDirective(directiveNode.Name.Value))
            {
                continue;
            }

            if (!applySerializeAsToScalars
                && DirectiveNames.SerializeAs.Name.Equals(directiveNode.Name.Value))
            {
                continue;
            }

            builder.Add(directiveNode);
        }

        return builder.ToImmutable();
    }
}
