using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.ModelContextProtocol.Extensions;

internal static class FieldNodeExtensions
{
    public static SelectionState GetSelectionState(
        this FieldNode fieldNode,
        ISyntaxNode? declaringNode,
        ITypeDefinition? parentType)
    {
        var skipField = fieldNode.GetSkipIfValue();
        var includeField = fieldNode.GetIncludeIfValue();

        object? skipFragment = null;
        object? includeFragment = null;
        var typeConditional = false;

        switch (declaringNode)
        {
            case FragmentSpreadNode fragmentSpread:
                skipFragment = fragmentSpread.GetSkipIfValue();
                includeFragment = fragmentSpread.GetIncludeIfValue();

                break;

            case InlineFragmentNode inlineFragment:
                skipFragment = inlineFragment.GetSkipIfValue();
                includeFragment = inlineFragment.GetIncludeIfValue();

                if (inlineFragment.TypeCondition is not null
                    && inlineFragment.TypeCondition.Name.Value != parentType?.Name)
                {
                    typeConditional = true;
                }

                break;
        }

        if (skipField is null or false
            && includeField is null or true
            && skipFragment is null or false
            && includeFragment is null or true
            && !typeConditional)
        {
            return SelectionState.Included;
        }

        if (skipField is true
            || includeField is false
            || skipFragment is true
            || includeFragment is false)
        {
            return SelectionState.Excluded;
        }

        // Variable skip/include or type condition not matching the parent type.
        return SelectionState.Conditional;
    }
}

internal enum SelectionState
{
    Included,
    Excluded,
    Conditional
}
