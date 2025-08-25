using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Represents an execution node for GraphQL node field queries in a composite schema.
/// This execution node handles the execution of queries that fetch entities by their global ID.
/// </summary>
public sealed class NodeExecutionNode : ExecutionNode
{
    private readonly Dictionary<string, ExecutionNode> _branches = [];
    private ExecutionNode _fallbackQuery = null!;
    private readonly string _responseName;
    private readonly IValueNode _idValue;

    internal NodeExecutionNode(
        int id,
        string responseName,
        IValueNode idValue)
    {
        _responseName = responseName;
        _idValue = idValue;
        Id = id;
    }

    /// <inheritdoc />
    public override int Id { get; }

    /// <inheritdoc />
    public override ExecutionNodeType Type => ExecutionNodeType.Node;

    /// <summary>
    /// Gets the possible type branches for the node field.
    /// The key is the type name and the value the node to execute for that type.
    /// </summary>
    public Dictionary<string, ExecutionNode> Branches => _branches;

    /// <summary>
    /// Get the value passed to the id argument of the node field.
    /// This can be a <see cref="VariableNode" /> or a <see cref="StringValueNode" />.
    /// </summary>
    public IValueNode IdValue => _idValue;

    /// <summary>
    /// Gets the response name for the node field.
    /// </summary>
    public string ResponseName => _responseName;

    /// <summary>
    /// Gets the fallback query for the case that a valid type name was requested,
    /// but we do not have a branch for that type.
    /// </summary>
    public ExecutionNode FallbackQuery => _fallbackQuery;

    protected override ValueTask<ExecutionStatus> OnExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
    {
        var id = IdValue switch
        {
            VariableNode variable => GetVariableValue(variable),
            StringValueNode stringValueNode => stringValueNode.Value,
            _ => throw new InvalidOperationException(
                $"Expected either a {nameof(VariableNode)} or {nameof(StringValueNode)}.")
        };

        if (!context.TryParseTypeNameFromId(id, out var typeName)
            || !context.TryGetNodeLookupSchemaForType(typeName, out var schemaName))
        {
            // We have an invalid id or a valid id of a type that does not implement the Node interface
            var error = ErrorBuilder.New()
                .SetMessage("The node ID string has an invalid format.")
                .SetExtension("originalValue", id)
                .Build();

            context.AddErrors(error, [ResponseName], Path.Root);

            return ValueTask.FromResult(ExecutionStatus.Failed);
        }

        if (_branches.TryGetValue(typeName, out var operation))
        {
            // We have a branch and we select it for exclusive execution
            EnqueueDependentForExecution(context, operation);

            return ValueTask.FromResult(ExecutionStatus.Success);
        }

        // We have a valid type, but no branch, so we execute the fallback query.
        EnqueueDependentForExecution(context, FallbackQuery);

        context.SetSchemaForOperationNode(FallbackQuery.Id, schemaName);

        return ValueTask.FromResult(ExecutionStatus.Success);

        string GetVariableValue(VariableNode variable)
        {
            var variableName = variable.Name.Value;
            if (!context.Variables.TryGetValue<StringValueNode>(variableName, out var stringValueNode))
            {
                throw new InvalidOperationException(
                    $"Expected to be able to get a string value for variable '{variableName}'");
            }

            return stringValueNode.Value;
        }
    }

    internal void AddBranch(string objectTypeName, ExecutionNode node)
    {
        ArgumentException.ThrowIfNullOrEmpty(objectTypeName);
        ArgumentNullException.ThrowIfNull(node);

        ExpectMutable();

        if (node.Equals(this))
        {
            throw new InvalidOperationException("A node can not be a branch of itself.");
        }

        _branches[objectTypeName] = node;
    }

    internal void AddFallbackQuery(ExecutionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        ExpectMutable();

        if (node.Equals(this))
        {
            throw new InvalidOperationException("A node can not be the fallback query of itself.");
        }

        _fallbackQuery = node;
    }
}
