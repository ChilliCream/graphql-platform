using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class IntrospectionExecutionNode : ExecutionNode
{
    private readonly Selection[] _selections;

    public IntrospectionExecutionNode(int id, Selection[] selections)
    {
        Id = id;
        _selections = selections;
    }

    public override int Id { get; }

    public override ReadOnlySpan<ExecutionNode> Dependencies => default;

    public override Task<ExecutionStatus> ExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
    {
        var resultPool = context.ResultPool;
        var backlog = new Stack<(object? Parent, Selection Selection, FieldResult Result)>();
        var root = context.ResultPool.RentObjectResult();
        var selectionSet = context.OperationPlan.Operation.RootSelectionSet;
        root.Initialize(resultPool, selectionSet, context.IncludeFlags, rawLeafFields: true);

        foreach (var selection in _selections)
        {
            if (selection.Resolver is null
                || !selection.Field.IsIntrospectionField
                || !selection.IsIncluded(context.IncludeFlags))
            {
                continue;
            }

            backlog.Push((null, selection, root[selection.ResponseName]));
        }

        ExecuteSelections(context, backlog);
        context.AddPartialResults(root, _selections);

        return Task.FromResult(new ExecutionStatus(Id, IsSkipped: false));
    }

    private static void ExecuteSelections(
        OperationPlanContext context,
        Stack<(object? Parent, Selection Selection, FieldResult Result)> backlog)
    {
        var operation = context.OperationPlan.Operation;
        var fieldContext = new ReusableFieldContext(
            context.Schema,
            context.Variables,
            context.IncludeFlags,
            context.ResultPool,
            context.CreateRentedBuffer());

        while (backlog.TryPop(out var current))
        {
            var (parent, selection, result) = current;
            fieldContext.Initialize(parent, selection, result);

            selection.Resolver?.Invoke(fieldContext);

            if (!selection.IsLeaf)
            {
                if (result is ObjectFieldResult { HasNullValue: false } objectFieldResult)
                {
                    var objectType = selection.Type.NamedType<IObjectTypeDefinition>();
                    var selectionSet = operation.GetSelectionSet(selection, objectType);
                    var objectResult = objectFieldResult.Value;
                    var insertIndex = 0;

                    for (var i = 0; i < selectionSet.Selections.Length; i++)
                    {
                        var childSelection = selectionSet.Selections[i];

                        if (!childSelection.IsIncluded(context.IncludeFlags))
                        {
                            continue;
                        }

                        backlog.Push((fieldContext.RuntimeResults[0], childSelection, objectResult.Fields[insertIndex++]));
                    }
                }
                else if (result is ListFieldResult { HasNullValue: false, Value: ObjectListResult list })
                {
                    var objectType = selection.Type.NamedType<IObjectTypeDefinition>();
                    var selectionSet = operation.GetSelectionSet(selection, objectType);

                    for (var i = 0; i < list.Items.Count; i++)
                    {
                        var objectResult = list.Items[i];
                        var runtimeResult = fieldContext.RuntimeResults[i];

                        if (objectResult is null)
                        {
                            continue;
                        }

                        var insertIndex = 0;

                        for (var j = 0; j < selectionSet.Selections.Length; j++)
                        {
                            var childSelection = selectionSet.Selections[j];

                            if (!childSelection.IsIncluded(context.IncludeFlags))
                            {
                                continue;
                            }

                            backlog.Push((runtimeResult, childSelection, objectResult.Fields[insertIndex++]));
                        }
                    }
                }
            }
        }
    }

    protected internal override void Seal()
    {
    }
}
