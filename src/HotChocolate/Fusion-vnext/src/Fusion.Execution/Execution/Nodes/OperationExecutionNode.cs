using System.Buffers;
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed record OperationExecutionNode : ExecutionNode
{
    public OperationExecutionNode(
        int id,
        OperationDefinitionNode operation,
        string schemaName,
        SelectionPath target,
        SelectionPath source,
        ImmutableArray<OperationRequirement> requirements)
        : base(id)
    {
        Operation = operation;
        SchemaName = schemaName;
        Target = target;
        Source = source;
        Requirements = requirements;

        // We compute the hash of the operation definition when it is set.
        // This hash can be used within the GraphQL client to identify the operation
        // and optimize request serialization.
        Span<byte> hash = stackalloc byte[16];
        var length = MD5.HashData(Encoding.UTF8.GetBytes(operation.ToString()), hash);
        OperationId = Convert.ToHexString(hash[..length]);

        var variables = ImmutableArray.CreateBuilder<string>();

        foreach (var variableDef in operation.VariableDefinitions)
        {
            if (requirements.Any(r => r.Key == variableDef.Variable.Name.Value))
            {
                continue;
            }

            variables.Add(variableDef.Variable.Name.Value);
        }

        Variables = variables.ToImmutable();
    }

    /// <summary>
    /// Gets the unique identifier of the operation.
    /// </summary>
    public string OperationId { get; }

    /// <summary>
    /// Gets the operation definition that this execution node represents.
    /// </summary>
    public OperationDefinitionNode Operation { get; }

    /// <summary>
    /// Gets the name of the source schema that this operation is executed against.
    /// </summary>
    public string SchemaName { get; }

    /// <summary>
    /// Gets the path to the selection set for which this operation fetches data.
    /// </summary>
    public SelectionPath Target { get; } = SelectionPath.Root;

    /// <summary>
    /// Gets the path to the local selection set (the selection set within the source schema request)
    /// to extract the data from.
    /// </summary>
    public SelectionPath Source { get; } = SelectionPath.Root;

    /// <summary>
    /// Gets the execution nodes that depend on this operation to be completed
    /// before they can be executed.
    /// </summary>
    public ImmutableArray<ExecutionNode> Dependents { get; init; } = [];

    /// <summary>
    /// Gets the data requirements that are needed to execute this operation.
    /// </summary>
    public ImmutableArray<OperationRequirement> Requirements { get; init; } = [];

    /// <summary>
    /// Gets the variables that are needed to execute this operation.
    /// </summary>
    public ImmutableArray<string> Variables { get; }

    public override async Task<ExecutionStatus> ExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
    {
        var variables = context.CreateVariableValueSets(Target, Variables, Requirements);

        if (variables.Length == 0 && (Requirements.Length > 0 || Variables.Length > 0))
        {
            return new ExecutionStatus(Id, IsSkipped: true);
        }

        var request = new SourceSchemaClientRequest
        {
            OperationId = OperationId,
            Operation = Operation,
            Variables = variables
        };

        var client = context.GetClient(SchemaName, Operation.Operation);
        var response = await client.ExecuteAsync(context, request, cancellationToken);

        if (response.IsSuccessful)
        {
            var index = 0;
            var bufferLength = variables.Length > 1 ? variables.Length : 1;
            var buffer = ArrayPool<SourceSchemaResult>.Shared.Rent(bufferLength);

            try
            {
                await foreach (var result in response.ReadAsResultStreamAsync(cancellationToken))
                {
                    buffer[index++] = result;
                }

                context.AddPartialResults(Source, buffer.AsSpan(0, index));
            }
            catch
            {
                // if there is an error, we need to make sure that the pooled buffers for the JsonDocuments
                // are returned to the pool.
                foreach (var result in buffer.AsSpan(0, index))
                {
                    result?.Dispose();
                }

                throw;
            }
            finally
            {
                buffer.AsSpan(0, index).Clear();
                ArrayPool<SourceSchemaResult>.Shared.Return(buffer);
            }
        }

        return new ExecutionStatus(Id, IsSkipped: false);
    }
}
