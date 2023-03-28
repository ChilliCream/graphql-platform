using System.Text.Json;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Execution;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

internal sealed class NodeResolverNode : QueryPlanNode
{
    private readonly Dictionary<string, QueryPlanNode> _fetchNodes = new(StringComparer.Ordinal);

    public NodeResolverNode(int id) : base(id)
    {

    }

    public override QueryPlanNodeKind Kind => QueryPlanNodeKind.NodeResolver;

    protected override Task OnExecuteNodesAsync(
        FusionExecutionContext context,
        IExecutionState state,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
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
