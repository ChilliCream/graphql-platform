using System.Collections.Immutable;
using System.Text.Json;
using HotChocolate.Fusion.Planning;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

public record OperationExecutionNode : ExecutionNode
{
    public required OperationDefinitionNode Definition { get; init; }

    public required string SchemaName { get; init; }

    public ImmutableArray<ExecutionNode> Dependents { get; init; } = [];

    public ImmutableArray<OperationRequirement> Requirements { get; init; } = [];

    public override async Task<ExecutionStatus> ExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
    {
        var variables = context.CreateVariables(Requirements);
        var client = context.GetClient(SchemaName);
        var result = await client.ExecuteAsync(null!, cancellationToken);

    }
}

public interface IGraphQLClient
{
    ValueTask<GraphQLClientResponse> ExecuteAsync(
        GraphQLClientRequest request,
        CancellationToken cancellationToken);
}

public abstract class GraphQLClientResponse
{

}

public abstract class GraphQLClientRequest {}

public sealed class GraphQLResult : IDisposable
{
    private readonly IDisposable? _resource = null;

    public int RequestIndex { get; }

    public JsonElement Data { get; }

    public JsonElement Errors { get; }

    public JsonElement Extensions { get; }

    public void Dispose()
    {
        _resource?.Dispose();
    }
}

