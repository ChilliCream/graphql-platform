using System.Collections.Concurrent;
using System.Text.Json;
using HotChocolate.Fusion.Execution;

namespace HotChocolate.Fusion.Planning;

internal sealed class If : QueryPlanNode
{
    private readonly List<Branch> _branches = new();

    public If(int id) : base(id)
    {
    }

    public override QueryPlanNodeKind Kind => QueryPlanNodeKind.If;

    protected override Task OnExecuteNodesAsync(
        FusionExecutionContext context,
        ExecutionState state,
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
            // TODO : error helper
            throw new InvalidOperationException("The execution node is read-only.");
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

    public readonly record struct Branch(string Key, object Value, QueryPlanNode Node);

    protected override void FormatNodesProperty(Utf8JsonWriter writer)
    {
        if (_branches.Count > 0)
        {
            writer.WritePropertyName("branches");

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
}
