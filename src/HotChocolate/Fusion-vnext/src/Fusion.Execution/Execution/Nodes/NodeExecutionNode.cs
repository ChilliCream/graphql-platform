using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class NodeExecutionNode(
    int id,
    string responseName,
    IValueNode idValue,
    OperationSourceText fallbackQuery) : ExecutionNode
{
    private readonly Dictionary<string, ExecutionNode> _branches = [];

    public override int Id { get; } = id;

    public override ExecutionNodeType Type => ExecutionNodeType.Node;

    public Dictionary<string, ExecutionNode> Branches => _branches;

    public IValueNode IdValue => idValue;

    public string ResponseName => responseName;

    public OperationSourceText FallbackQuery { get; } = fallbackQuery;

    // TODO: This should be computed at schema generation and placed as metadata on the schema definition
    private readonly Dictionary<string, string> _typeNameToSchemaLookup = new Dictionary<string, string>
    {
        ["Discussion"] = "a"
    };

    public override async ValueTask<ExecutionStatus> OnExecuteAsync(
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
            || !_typeNameToSchemaLookup.TryGetValue(typeName, out var schema))
        {
            // We have an invalid id or a valid id of a type that does not implement the Node interface
            var error = ErrorBuilder.New()
                .SetMessage("Invalid id") // TODO: Better error
                .Build();

            context.AddErrors(error, [ResponseName], Path.Root);

            return ExecutionStatus.Failed;
        }

        if (_branches.TryGetValue(typeName, out var operation))
        {
            // We have a branch and we select it for exclusive execution
            EnqueueDependentForExecution(context, operation);

            return ExecutionStatus.Success;
        }

        // We have a valid type, but no branch, so we execute the fallback operation.

        // TODO: Constructing this on the fly is bad, mkay
        var fallbackExecutionNode = new OperationExecutionNode(
            Id + 1, // TODO: This is wrong
            FallbackQuery,
            schema,
            SelectionPath.Root,
            SelectionPath.Root,
            [],
            [], // TODO
            [ResponseName]
        );

        // TODO: We need to skip all branch nodes in this case

        return await fallbackExecutionNode.OnExecuteAsync(context, cancellationToken);

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

    // TODO: Handle sealing
    internal void AddBranch(string objectTypeName, ExecutionNode node)
    {
        ArgumentException.ThrowIfNullOrEmpty(objectTypeName);
        ArgumentNullException.ThrowIfNull(node);

        if (node.Equals(this))
        {
            throw new InvalidOperationException("A node can not be a branch of itself.");
        }

        _branches[objectTypeName] = node;
    }
}
