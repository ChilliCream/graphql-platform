using System.Collections.Immutable;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes;

internal static class SyntaxNodeHelpers
{
    public static SelectionSetNode ToSyntaxNode(this IReadOnlyList<SelectionPlanNode> selections)
    {
        var selectionSet = ImmutableArray.CreateBuilder<ISelectionNode>();

        foreach (var selection in selections)
        {
            selectionSet.Add(selection switch
            {
                FieldPlanNode field => field.ToSyntaxNode(),
                InlineFragmentPlanNode inlineFragment => inlineFragment.ToSyntaxNode(),
                _ => throw new InvalidOperationException()
            });
        }

        return new SelectionSetNode(null, selectionSet.ToImmutable());
    }

    public static IReadOnlyList<DirectiveNode> ToSyntaxNode(this IReadOnlyList<CompositeDirective> directives)
    {
        var directiveNodes = ImmutableArray.CreateBuilder<DirectiveNode>();

        foreach (var directive in directives)
        {
            directiveNodes.Add(directive.ToSyntaxNode());
        }

        return directiveNodes.ToImmutable();
    }

    public static IReadOnlyList<ArgumentNode> ToSyntaxNode(this IReadOnlyList<ArgumentAssignment> arguments)
    {
        var argumentNodes = ImmutableArray.CreateBuilder<ArgumentNode>();

        foreach (var argument in arguments)
        {
            argumentNodes.Add(argument.ToSyntaxNode());
        }

        return argumentNodes.ToImmutable();
    }
}
