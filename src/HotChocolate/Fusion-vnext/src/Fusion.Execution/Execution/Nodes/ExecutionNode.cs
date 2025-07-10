using HotChocolate.Buffers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

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
    private Selection[] _selections;

    public override int Id { get; }

    public override ReadOnlySpan<ExecutionNode> Dependencies => default;

    public override async Task<ExecutionStatus> ExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
    {
        var resultPoolSession = context.ResultPoolSession;
        var memory = context.CreateRentedBuffer();

        foreach (var selection in _selections)
        {
            if (selection.Resolver is null)
            {
                continue;
            }

            FieldResult result = selection.Field.Name.Equals(IntrospectionFieldNames.TypeName)
                ? new RawFieldResult()
                : resultPoolSession.RentObjectFieldResult();

            await selection.Resolver(
                new FieldContext(
                    memory,
                    context.Schema,
                    selection,
                    result),
                cancellationToken);

            

            // copy result
        }

        return new ExecutionStatus(Id, IsSkipped: false);
    }

    protected internal override void Seal()
    {
        throw new NotImplementedException();
    }
}
