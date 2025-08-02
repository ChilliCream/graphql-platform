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

        // var variables = context.OperationContext.Variables;
        // var coercedArguments = new Dictionary<string, ArgumentValue>();
        //
        // _selection.Arguments.CoerceArguments(variables, coercedArguments);
        //
        // var idArgument = coercedArguments["id"];
        //
        // if (idArgument.ValueLiteral is not StringValueNode formattedId)
        // {
        //     context.Result.AddError(InvalidNodeFormat(_selection), _selection);
        //     return;
        // }
        //
        // string typeName;
        //
        // try
        // {
        //     typeName = context.ParseTypeNameFromId(formattedId.Value);
        // }
        // catch (Exception exception)
        // {
        //     context.Result.AddError(InvalidNodeFormat(_selection, exception), _selection);
        //     return;
        // }
        //
        // if(!_fetchNodes.TryGetValue(typeName, out var fetchNode))
        // {
        //     context.Result.AddError(InvalidNodeFormat(_selection), _selection);
        //     return;
        // }
        //
        // await fetchNode.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);

        var result = new ExecutionNodeResult(
            Id,
            Activity.Current,
            ExecutionStatus.Success,
            Stopwatch.GetElapsedTime(start));

        return Task.FromResult(result);
    }

    protected internal override void Seal()
    {
    }
}
