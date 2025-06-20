using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

public record IntrospectionExecutionNode(
    int Id,
    OperationDefinitionNode Operation) : ExecutionNode(Id)
{
    public override Task<ExecutionStatus> ExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
