using System.Collections.Concurrent;
using System.Text.Json;
using HotChocolate.Fusion.Execution;
using static HotChocolate.Fusion.Planning.Utf8QueryPlanPropertyNames;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// The <see cref="If"/> node is responsible for executing a node based on a state.
/// </summary>
internal sealed class If : QueryPlanNode
{
    private readonly List<Branch> _branches = new();

    /// <summary>
    /// Initializes a new instance of <see cref="If"/>.
    /// </summary>
    /// <param name="id">
    /// The unique id of this node.
    /// </param>
    public If(int id) : base(id)
    {
    }

    /// <summary>
    /// Gets the kind of this node.
    /// </summary>
    public override QueryPlanNodeKind Kind => QueryPlanNodeKind.If;

    protected override Task OnExecuteNodesAsync(
        FusionExecutionContext context,
        RequestState state,
        CancellationToken cancellationToken)
    {
        var contextData = (ConcurrentDictionary<string, object?>)context.OperationContext.ContextData;

        if(_branches.Count == 0)
        {
            var branch = _branches[0];
            if(contextData.TryGetValue(branch.Key, out var value) && branch.Value.Equals(value))
            {
                return branch.Node.ExecuteAsync(context, cancellationToken);
            }

            return Task.CompletedTask;
        }

        List<Task>? tasks = null;

        foreach (var branch in _branches)
        {
            if(contextData.TryGetValue(branch.Key, out var value) && branch.Value.Equals(value))
            {
                (tasks ??= new()).Add(branch.Node.ExecuteAsync(context, cancellationToken));
            }
        }

        return tasks is null ? Task.CompletedTask : Task.WhenAll(tasks);
    }

    public void AddBranch(string key, object value, QueryPlanNode node)
    {
        if (IsReadOnly)
        {
            throw ThrowHelper.Node_ReadOnly();
        }

        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        _branches.Add(new Branch(key, value, node));
        base.AddNode(node);
    }

    internal override void AddNode(QueryPlanNode node)
        => throw new NotSupportedException();

    protected override void FormatNodesProperty(Utf8JsonWriter writer)
    {
        if (_branches.Count > 0)
        {
            writer.WritePropertyName(BranchesProp);

            writer.WriteStartArray();

            foreach (var branch in _branches)
            {
                writer.WriteStartObject();
                writer.WriteString("state", branch.Key);
                writer.WriteString("equalsTo", branch.Value.ToString());
                writer.WritePropertyName("node");
                branch.Node.Format(writer);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }
    }
    
    private readonly record struct Branch(string Key, object Value, QueryPlanNode Node);
}
