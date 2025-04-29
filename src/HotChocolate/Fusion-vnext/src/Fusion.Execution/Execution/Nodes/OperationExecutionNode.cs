using System.Collections.Immutable;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

public record OperationExecutionNode : ExecutionNode
{
    public string OperationId { get; private set; } = string.Empty;

    public required OperationDefinitionNode Operation
    {
        get;
        init
        {
            field = value;

            // We compute the hash of the operation definition when it is set.
            // This hash can be used within the GraphQL client to identify the operation
            // and optimize request serialization.
            Span<byte> hash = stackalloc byte[16];
            var length = MD5.HashData(Encoding.UTF8.GetBytes(value.ToString()), hash);
            hash = hash[..length];
            OperationId = Convert.ToHexString(hash);
        }
    }

    public required string SchemaName { get; init; }

    /// <summary>
    /// Gets the path to the selection set for which this operation fetches data.
    /// </summary>
    public SelectionPath Target { get; init; } = SelectionPath.Root;

    /// <summary>
    /// Gets the path to the local selection set (the selection set within the source schema request)
    /// to extract the data from.
    /// </summary>
    public SelectionPath Source { get; init; } = SelectionPath.Root;

    /// <summary>
    /// Gets the execution nodes that depend on this operation to be completed
    /// before they can be executed.
    /// </summary>
    public ImmutableArray<ExecutionNode> Dependents { get; init; } = [];

    /// <summary>
    /// Gets the data requirements that are needed to execute this operation.
    /// </summary>
    public ImmutableArray<OperationRequirement> Requirements { get; init; } = [];

    public override async Task<ExecutionStatus> ExecuteAsync(
        OperationPlanContext context,
        Path executionPath,
        CancellationToken cancellationToken = default)
    {
        var request = new GraphQLClientRequest
        {
            OperationId = OperationId,
            Operation = Operation,
            Variables = context.CreateVariables(Requirements),
        };

        var client = context.GetClient(SchemaName);
        var response = await client.ExecuteAsync(request, cancellationToken);

        if (response.IsSuccessful)
        {
            await foreach (var result in response.ReadResultsAsync().WithCancellation(cancellationToken))
            {
                var fetchResult = FetchResult.From(this, result);
                context.ResultStore.Add(fetchResult);
            }
        }

        return new ExecutionStatus(Id);
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
    public abstract IAsyncEnumerable<GraphQLResult> ReadResultsAsync();

    public abstract bool IsSuccessful { get; }
}

public sealed class GraphQLClientRequest
{
    public required string OperationId { get; init; }

    public required OperationDefinitionNode Operation { get; init; }

    public ImmutableArray<VariableValues> Variables { get; init; } = [];
}

public sealed class GraphQLResult : IDisposable
{
    private readonly IDisposable? _resource;

    public GraphQLResult(Path path, JsonDocument document)
    {
        _resource = document;
        Path = path;

        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if(root.TryGetProperty("data", out var data))
        {
            Data = data;
        }

        if (root.TryGetProperty("errors", out var errors))
        {
            Errors = errors;
        }

        if (root.TryGetProperty("extensions", out var extensions))
        {
            Extensions = extensions;
        }
    }

    public GraphQLResult(Path path, IDisposable resource, JsonElement data, JsonElement errors, JsonElement extensions)
    {
        _resource = resource;
        Path = path;
        Data = data;
        Errors = errors;
        Extensions = extensions;
    }

    public Path Path { get; }

    public JsonElement Data { get; }

    public JsonElement Errors { get; }

    public JsonElement Extensions { get; }

    public void Dispose() => _resource?.Dispose();
}
