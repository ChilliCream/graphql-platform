using System.Text.Json;
using HotChocolate.Fusion.Execution;

namespace HotChocolate.Fusion.Planning;

internal abstract class QueryPlanNode
{
    private readonly List<QueryPlanNode> _nodes = new();
    private bool _isReadOnly;

    protected QueryPlanNode(int id)
    {
        Id = id;
    }

    public int Id { get; }

    public abstract QueryPlanNodeKind Kind { get; }

    public IReadOnlyList<QueryPlanNode> Nodes => _nodes;

    internal async Task ExecuteAsync(
        IFederationContext context,
        CancellationToken cancellationToken)
    {
        var state = context.State;

        await OnExecuteAsync(context, state, cancellationToken).ConfigureAwait(false);

        if (_nodes.Count > 0)
        {
            await OnExecuteNodesAsync(context, state, cancellationToken).ConfigureAwait(false);
        }
    }

    protected virtual Task OnExecuteAsync(
        IFederationContext context,
        IExecutionState state,
        CancellationToken cancellationToken)
        => Task.CompletedTask;

    protected virtual async Task OnExecuteNodesAsync(
        IFederationContext context,
        IExecutionState state,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < _nodes.Count; i++)
        {
            await _nodes[i].ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
        }
    }

    internal void AddNode(QueryPlanNode node)
    {
        if (_isReadOnly)
        {
            throw new InvalidOperationException("The execution node is read-only.");
        }

        if (!_nodes.Contains(node))
        {
            _nodes.Add(node);
        }
    }

    internal void Seal()
    {
        if (!_isReadOnly)
        {
            OnSeal();
            _isReadOnly = true;
        }
    }

    protected virtual void OnSeal() { }

    internal void Format(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("type", Kind.ToString());
        FormatNodesProperty(writer);
        FormatProperties(writer);
        writer.WriteEndObject();
    }

    protected virtual void FormatProperties(Utf8JsonWriter writer)
    {
    }

    private void FormatNodesProperty(Utf8JsonWriter writer)
    {
        if (_nodes.Count > 0)
        {
            writer.WritePropertyName("nodes");

            writer.WriteStartArray();

            foreach (var node in _nodes)
            {
                node.Format(writer);
            }

            writer.WriteEndArray();
        }
    }
}
