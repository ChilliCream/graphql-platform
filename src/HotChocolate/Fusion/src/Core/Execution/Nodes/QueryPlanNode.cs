using System.Runtime.InteropServices;
using System.Text.Json;
using static HotChocolate.Fusion.Utilities.Utf8QueryPlanPropertyNames;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// The base class for all query plan nodes.
/// </summary>
internal abstract class QueryPlanNode
{
    private readonly List<QueryPlanNode> _nodes = [];
    private bool _isReadOnly;

    /// <summary>
    /// Initializes a new instance of <see cref="QueryPlanNode"/>.
    /// </summary>
    /// <param name="id">
    /// The unique id of this node.
    /// </param>
    protected QueryPlanNode(int id)
    {
        Id = id;
    }

    /// <summary>
    /// Gets the unique id of this node.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the kind of this node.
    /// </summary>
    public abstract QueryPlanNodeKind Kind { get; }

    /// <summary>
    /// Gets the child nodes of this node.
    /// </summary>
    public IReadOnlyList<QueryPlanNode> Nodes => _nodes;

    /// <summary>
    /// Gets a value indicating whether this node is read-only.
    /// </summary>
    private protected bool IsReadOnly => _isReadOnly;

    internal async Task ExecuteAsync(
        FusionExecutionContext context,
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
        FusionExecutionContext context,
        RequestState state,
        CancellationToken cancellationToken)
        => Task.CompletedTask;

    protected virtual async Task OnExecuteNodesAsync(
        FusionExecutionContext context,
        RequestState state,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < _nodes.Count; i++)
        {
            await _nodes[i].ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
        }
    }

    internal virtual void AddNode(QueryPlanNode node)
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
        if (_isReadOnly)
        {
            return;
        }

        OnSeal();

        foreach (var node in Nodes)
        {
            node.Seal();
        }

        _isReadOnly = true;
    }

    protected virtual void OnSeal() { }

    internal void Format(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString(TypeProp, Kind.Format());
        FormatProperties(writer);
        FormatNodesProperty(writer);
        writer.WriteEndObject();
    }

    protected virtual void FormatProperties(Utf8JsonWriter writer)
    {
    }

    protected virtual void FormatNodesProperty(Utf8JsonWriter writer)
    {
        if (_nodes.Count <= 0)
        {
            return;
        }

        writer.WritePropertyName(NodesProp);
        writer.WriteStartArray();

        foreach (var node in _nodes)
        {
            node.Format(writer);
        }

        writer.WriteEndArray();
    }

    protected ReadOnlySpan<QueryPlanNode> GetNodesSpan()
        => CollectionsMarshal.AsSpan(_nodes);
}
