using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Introspection;

internal static class FieldContextExtensions
{
    public static ObjectResult RentInitializedObjectResult(this FieldContext context)
    {
        var selection = context.Selection;
        var operation = selection.DeclaringSelectionSet.DeclaringOperation;
        var selectionSetType = selection.Field.Type.NamedType().ExpectObjectType();
        var selectionSet = operation.GetSelectionSet(context.Selection, selectionSetType);
        var selectionSetResult = context.ResultPool.RentObjectResult();
        selectionSetResult.Initialize(context.ResultPool, selectionSet, context.IncludeFlags, rawLeafFields: true);
        return selectionSetResult;
    }
}
