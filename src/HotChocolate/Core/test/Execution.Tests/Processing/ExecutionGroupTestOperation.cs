using HotChocolate.Execution.ExecutionGroups;
using HotChocolate.Language;

namespace HotChocolate.Execution.Processing;

internal static class ExecutionGroupTestOperation
{
    public static async Task<(Operation Operation, SelectionSet RootSet, ExecutionGroupPlan Plan)> CompileAsync(
        string query)
    {
        var executor = await ExecutionGroupTestSchema.CreateAsync(new ExecutionScript());
        var schema = Assert.IsType<Schema>(executor.Schema);
        var operation = OperationCompiler.Compile("opid", Utf8GraphQLParser.Parse(query), schema);
        var root = operation.RootSelectionSet.Selections[0];
        var selectionSet = operation.GetSelectionSet(root);
        return (operation, selectionSet, selectionSet.ExecutionGroupPlan!);
    }
}
