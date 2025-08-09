using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.ModelContextProtocol.WellKnownDirectiveNames;

namespace HotChocolate.ModelContextProtocol.Extensions;

internal static class SelectionExtensions
{
    public static SelectionState GetSelectionState(this ISelection selection)
    {
        var skip =
            selection.SyntaxNode.Directives.FirstOrDefault(d => d.Name.Value == Skip)?
                .GetArgumentValue(WellKnownArgumentNames.If);

        var include =
            selection.SyntaxNode.Directives.FirstOrDefault(d => d.Name.Value == Include)?
                .GetArgumentValue(WellKnownArgumentNames.If);

        if (skip is null or BooleanValueNode { Value: false }
            && include is null or BooleanValueNode { Value: true })
        {
            return SelectionState.Included;
        }

        if (skip is BooleanValueNode { Value: true }
            || include is BooleanValueNode { Value: false })
        {
            return SelectionState.Excluded;
        }

        return SelectionState.Conditional;
    }
}

internal enum SelectionState
{
    Included,
    Excluded,
    Conditional
}
