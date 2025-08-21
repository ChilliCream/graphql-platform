using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class NodeExecutionNode(
    int id,
    string responseName,
    IValueNode idValue) : ExecutionNode
{
    private readonly Dictionary<string, ExecutionNode> _branches = [];
    private ExecutionNode _fallbackQuery = null!;

    public override int Id { get; } = id;

    public override ExecutionNodeType Type => ExecutionNodeType.Node;

    public Dictionary<string, ExecutionNode> Branches => _branches;

    public IValueNode IdValue => idValue;

    public string ResponseName => responseName;

    public ExecutionNode FallbackQuery => _fallbackQuery;

    // TODO: This should be computed at schema generation and placed as metadata on the schema definition
    private readonly Dictionary<string, string> _typeNameToSchemaLookup = new()
    {
        ["Discussion"] = "a"
    };

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
            || !_typeNameToSchemaLookup.TryGetValue(typeName, out var schemaName))
        {
            // We have an invalid id or a valid id of a type that does not implement the Node interface
            var error = ErrorBuilder.New()
                .SetMessage("Invalid id") // TODO: Better error
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
