using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using HotChocolate.Fusion.Execution.Clients;
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
        CancellationToken cancellationToken = default)
    {
        var request = new SourceSchemaClientRequest
        {
            OperationId = OperationId,
            Operation = Operation,
            Variables = context.CreateVariables(Requirements),
        };

        var client = context.GetClient(SchemaName);
        var response = await client.ExecuteAsync(request, cancellationToken);

        if (response.IsSuccessful)
        {
            await foreach (var result in response.ReadAsResultStreamAsync(cancellationToken))
            {
                var fetchResult = FetchResult.From(this, result);
                context.ResultStore.Add(fetchResult);
            }
        }

        return new ExecutionStatus(Id);
    }
}
