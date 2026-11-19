using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Fusion.Suites.SkipEntityFetch.B;

internal static class EntitySelection
{
    /// <summary>
    /// Returns whether the entity selection selects a field other than <c>__typename</c>
    /// once <c>@skip</c> and <c>@include</c> are applied with the request variables.
    /// </summary>
    public static bool SelectsAnyField(IResolverContext context)
        => SelectsAnyField(context.Selection.SyntaxNodes[0].Node.SelectionSet, context);

    private static bool SelectsAnyField(SelectionSetNode? selectionSet, IResolverContext context)
    {
        if (selectionSet is null)
        {
            return false;
        }

        foreach (var selection in selectionSet.Selections)
        {
            if (!IsIncluded(selection.Directives, context))
            {
                continue;
            }

            var selectsField = selection switch
            {
                FieldNode field => field.Name.Value != "__typename",
                InlineFragmentNode inlineFragment
                    => SelectsAnyField(inlineFragment.SelectionSet, context),
                FragmentSpreadNode spread
                    => SelectsAnyField(GetFragment(spread.Name.Value, context)?.SelectionSet, context),
                _ => false
            };

            if (selectsField)
            {
                return true;
            }
        }

        return false;
    }

    private static FragmentDefinitionNode? GetFragment(string name, IResolverContext context)
    {
        foreach (var definition in context.Operation.Document.Definitions)
        {
            if (definition is FragmentDefinitionNode fragment
                && string.Equals(fragment.Name.Value, name, StringComparison.Ordinal))
            {
                return fragment;
            }
        }

        return null;
    }

    private static bool IsIncluded(IReadOnlyList<DirectiveNode> directives, IResolverContext context)
    {
        if (GetCondition(directives, "skip", context) is true)
        {
            return false;
        }

        return GetCondition(directives, "include", context) is not false;
    }

    private static bool? GetCondition(
        IReadOnlyList<DirectiveNode> directives,
        string directiveName,
        IResolverContext context)
    {
        foreach (var directive in directives)
        {
            if (!string.Equals(directive.Name.Value, directiveName, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var argument in directive.Arguments)
            {
                if (!string.Equals(argument.Name.Value, "if", StringComparison.Ordinal))
                {
                    continue;
                }

                return argument.Value switch
                {
                    BooleanValueNode literal => literal.Value,
                    VariableNode variable
                        => context.Variables.GetValue<BooleanValueNode>(variable.Name.Value).Value,
                    _ => null
                };
            }
        }

        return null;
    }
}
