using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Nodes;

public abstract class ExecutionNode : IEquatable<ExecutionNode>
{
    public abstract int Id { get; }

    public abstract ReadOnlySpan<ExecutionNode> Dependencies { get; }

    public abstract Task<ExecutionStatus> ExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default);

    public bool Equals(ExecutionNode? other)
    {
        if (other is null)
        {
            return false;
        }

        return Id == other.Id;
    }

    public override bool Equals(object? obj)
        => Equals(obj as ExecutionNode);

    public override int GetHashCode()
        => Id;

    protected internal abstract void Seal();
}

public sealed class IntrospectionNode : ExecutionNode
{
    private readonly Selection[] _selections;

    public IntrospectionNode(int id, Selection[] selections)
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
        var resultPoolSession = context.ResultPoolSession;
        var backlog = new Stack<(object? Parent, Selection Selection, FieldResult Result)>();

        foreach (var selection in _selections)
        {
            if (selection.Resolver is null
                || !selection.Field.IsIntrospectionField
                || !selection.IsIncluded(context.IncludeFlags))
            {
                continue;
            }

            FieldResult result = selection.Field.Name.Equals(IntrospectionFieldNames.TypeName)
                ? new RawFieldResult()
                : resultPoolSession.RentObjectFieldResult();

            backlog.Push((null!, selection, result));
        }

        ExecuteSelections(context, backlog);

        // copy result

        return Task.FromResult(new ExecutionStatus(Id, IsSkipped: false));
    }

    private static void ExecuteSelections(
        OperationPlanContext context,
        Stack<(object? Parent, Selection Selection, FieldResult Result)> backlog)
    {
        var operation = context.OperationPlan.Operation;
        var fieldContext = new ReusableFieldContext(context.CreateRentedBuffer(), context.Schema);

        while (backlog.TryPop(out var current))
        {
            var (parent, selection, result) = current;
            fieldContext.Initialize(parent, selection, result);

            selection.Resolver?.Invoke(fieldContext);

            if (!selection.IsLeaf && result is ObjectFieldResult { HasNullValue: false } objectFieldResult)
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

                    backlog.Push((fieldContext.RuntimeResults, childSelection,  objectResult.Fields[insertIndex++]));
                }
            }
        }
    }

    protected internal override void Seal()
    {
    }
}
