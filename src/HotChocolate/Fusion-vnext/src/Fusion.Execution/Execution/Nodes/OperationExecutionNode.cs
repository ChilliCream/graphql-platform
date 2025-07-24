using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class OperationExecutionNode : ExecutionNode
{
    private readonly OperationRequirement[] _requirements;
    private readonly string[] _variables;
    private ExecutionNode[] _dependencies = [];
    private ExecutionNode[] _dependents = [];
    private int _dependencyCount;
    private int _dependentCount;
    private bool _isSealed;

    public OperationExecutionNode(
        int id,
        OperationDefinitionNode operation,
        string schemaName,
        SelectionPath target,
        SelectionPath source,
        OperationRequirement[] requirements)
    {
        Id = id;
        Operation = operation;
        SchemaName = schemaName;
        Target = target;
        Source = source;
        _requirements = requirements;

        // We compute the hash of the operation definition when it is set.
        // This hash can be used within the GraphQL client to identify the operation
        // and optimize request serialization.
        Span<byte> hash = stackalloc byte[16];
        var length = MD5.HashData(Encoding.UTF8.GetBytes(operation.ToString()), hash);
        OperationId = Convert.ToHexString(hash[..length]);

        var variables = new List<string>();

        foreach (var variableDef in operation.VariableDefinitions)
        {
            if (requirements.Any(r => r.Key == variableDef.Variable.Name.Value))
            {
                continue;
            }

            variables.Add(variableDef.Variable.Name.Value);
        }

        _variables = variables.ToArray();
    }

    public override int Id { get; }

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
    public SelectionPath Target { get; }

    /// <summary>
    /// Gets the path to the local selection set (the selection set within the source schema request)
    /// to extract the data from.
    /// </summary>
    public SelectionPath Source { get; }

    /// <summary>
    /// Gets the execution nodes that depend on this operation to be completed
    /// before they can be executed.
    /// </summary>
    public ReadOnlySpan<ExecutionNode> Dependents => _dependents;

    /// <summary>
    /// Gets the execution nodes that this operation depends on.
    /// </summary>
    public override ReadOnlySpan<ExecutionNode> Dependencies => _dependencies;

    /// <summary>
    /// Gets the data requirements that are needed to execute this operation.
    /// </summary>
    public ReadOnlySpan<OperationRequirement> Requirements => _requirements;

    /// <summary>
    /// Gets the variables that are needed to execute this operation.
    /// </summary>
    public ReadOnlySpan<string> Variables => _variables;

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

    internal void AddDependency(ExecutionNode node)
    {
        if (_isSealed)
        {
            throw new InvalidOperationException("The operation execution node is already sealed.");
        }

        ArgumentNullException.ThrowIfNull(node);

        if (node == this)
        {
            throw new InvalidOperationException("An operation cannot depend on itself.");
        }

        if (_dependencies.Length == 0)
        {
            _dependencies = new ExecutionNode[4];
        }

        if (_dependencyCount == _dependencies.Length)
        {
            Array.Resize(ref _dependencies, _dependencyCount * 2);
        }

        _dependencies[_dependencyCount++] = node;
    }

    internal void AddDependent(ExecutionNode node)
    {
        if (_isSealed)
        {
            throw new InvalidOperationException("The operation execution node is already sealed.");
        }

        ArgumentNullException.ThrowIfNull(node);

        if (node.Equals(this))
        {
            throw new InvalidOperationException("An operation cannot depend on itself.");
        }

        if (_dependents.Length == 0)
        {
            _dependents = new ExecutionNode[4];
        }

        if (_dependentCount == _dependents.Length)
        {
            Array.Resize(ref _dependents, _dependentCount * 2);
        }

        _dependents[_dependentCount++] = node;
    }

    protected internal override void Seal()
    {
        if (_isSealed)
        {
            throw new InvalidOperationException("The operation execution node is already sealed.");
        }

        if (_dependencies.Length > _dependencyCount)
        {
            Array.Resize(ref _dependencies, _dependencyCount);
        }

        if (_dependents.Length > _dependentCount)
        {
            Array.Resize(ref _dependents, _dependentCount);
        }

        _isSealed = true;
    }
}
