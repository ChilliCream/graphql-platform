using System.Diagnostics;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class NodeExecutionNode(int id) : ExecutionNode
{
    private readonly Dictionary<string, OperationExecutionNode> _branches = new();

    public override int Id { get; } = id;

    public override ReadOnlySpan<ExecutionNode> Dependencies => default;

    public Dictionary<string, OperationExecutionNode> Branches => _branches;

    public override Task<ExecutionNodeResult> ExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();

        const string idValue = "string";

        if (!context.TryParseTypeNameFromId(idValue, out var typeName))
        {
            // TODO: Create GraphQL error

            return Task.FromResult(new ExecutionNodeResult(
                Id,
                Activity.Current,
                ExecutionStatus.Failed,
                Stopwatch.GetElapsedTime(start)));
        }

        if (_branches.TryGetValue(typeName, out var operation))
        {
            // TODO: Skip all other branch nodes that aren't the selected one

            return Task.FromResult(new ExecutionNodeResult(
                Id,
                Activity.Current,
                ExecutionStatus.Success,
                Stopwatch.GetElapsedTime(start)));
        }

        if (context.Schema.Features.TryGet<object>(out var obj))
        {

        }


         // TODO: Create GraphQL error

         return Task.FromResult(new ExecutionNodeResult(
             Id,
             Activity.Current,
             ExecutionStatus.Failed,
             Stopwatch.GetElapsedTime(start)));
    }

    protected internal override void Seal()
    {
    }
}
