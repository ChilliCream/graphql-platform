using System.Text.Json;
using HotChocolate.Execution.Internal;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Relay;

namespace HotChocolate.Fusion.Planning;

internal sealed class NodeResolverNode : QueryPlanNode
{
    private readonly Dictionary<string, QueryPlanNode> _fetchNodes = new(StringComparer.Ordinal);

    public NodeResolverNode(int id, ISelection selection) : base(id)
    {
        Selection = selection;
    }

    public override QueryPlanNodeKind Kind => QueryPlanNodeKind.NodeResolver;

    public ISelection Selection { get; }

    protected override async Task OnExecuteNodesAsync(
        FusionExecutionContext context,
        IExecutionState state,
        CancellationToken cancellationToken)
    {
        var variables = context.OperationContext.Variables;
        var coercedArguments = new Dictionary<string, ArgumentValue>();

        Selection.Arguments.CoerceArguments(variables, coercedArguments);

        var idArgument = coercedArguments["id"];

        if (idArgument.ValueLiteral is not StringValueNode formattedId)
        {
            // TODO : ERROR HELPER
            context.Result.AddError(
                ErrorBuilder.New()
                    .SetMessage("Node id format is invalid!")
                    .AddLocation(Selection.SyntaxNode)
                    .Build(),
                Selection);
            return;
        }

        IdValue idValue;

        try
        {
            idValue = context.ParseId(formattedId.Value);
        }
        catch (IdSerializationException ex)
        {
            // TODO : ERROR HELPER
            context.Result.AddError(
                ErrorBuilder.New()
                    .SetMessage("Node id format is invalid 2!")
                    .AddLocation(Selection.SyntaxNode)
                    .SetException(ex)
                    .Build(),
                Selection);
            return;
        }

        if(!_fetchNodes.TryGetValue(idValue.TypeName, out var fetchNode))
        {
            // TODO : ERROR HELPER
            context.Result.AddError(
                ErrorBuilder.New()
                    .SetMessage("The id is invalid 3!")
                    .AddLocation(Selection.SyntaxNode)
                    .Build(),
                Selection);
            return;
        }

        await fetchNode.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
    }

    public void AddNode(string entityTypeName, QueryPlanNode fetchNode)
    {
        if (_fetchNodes.ContainsKey(entityTypeName))
        {
            throw new ArgumentException(
                "A fetch node for this entity type already exists.",
                paramName: nameof(entityTypeName));
        }

        _fetchNodes.Add(entityTypeName, fetchNode);
        base.AddNode(fetchNode);
    }

    protected override void FormatProperties(Utf8JsonWriter writer)
    {
        writer.WriteNumber("selectionId", Selection.Id);
        writer.WriteString("responseName", Selection.ResponseName);
        base.FormatProperties(writer);
    }

    protected override void FormatNodesProperty(Utf8JsonWriter writer)
    {
        if (_fetchNodes.Count > 0)
        {
            writer.WritePropertyName("branches");

            writer.WriteStartArray();

            foreach (var (type, node) in _fetchNodes)
            {
                writer.WriteStartObject();
                writer.WriteString("type", type);
                writer.WritePropertyName("node");
                node.Format(writer);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }
    }
}
