using System.Text.Json;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Execution;
using static HotChocolate.Fusion.Execution.ExecutorUtils;

namespace HotChocolate.Fusion.Planning;

internal sealed class ComposeNode : QueryPlanNode
{
    private readonly IReadOnlyList<ISelectionSet> _selectionSets;

    public ComposeNode(int id, ISelectionSet selectionSet)
        : this(id, new[] { selectionSet })
    {
    }

    public ComposeNode(int id, IReadOnlyList<ISelectionSet> selectionSets) : base(id)
    {
        _selectionSets = selectionSets ?? throw new ArgumentNullException(nameof(selectionSets));
    }

    public override QueryPlanNodeKind Kind => QueryPlanNodeKind.Compose;

    protected override Task OnExecuteAsync(
        IFederationContext context,
        IExecutionState state,
        CancellationToken cancellationToken)
    {
        if (_selectionSets.Count == 1)
        {
            if (state.TryGetState(_selectionSets[0], out var values))
            {
                for (var i = 0; i < values.Count; i++)
                {
                    ComposeResult(context, values[i]);
                }
            }
        }
        else
        {
            var aggregated = new List<WorkItem>();

            foreach (var selectionSet in _selectionSets)
            {
                if (state.TryGetState(selectionSet, out var values))
                {
                    aggregated.AddRange(values);
                }
            }

            if (aggregated.Count > 0)
            {
                for (var i = 0; i < aggregated.Count; i++)
                {
                    ComposeResult(context, aggregated[i]);
                }
            }
        }

        return Task.CompletedTask;
    }

    protected override async Task OnExecuteNodesAsync(
        IFederationContext context,
        IExecutionState state,
        CancellationToken cancellationToken)
    {
        if (_selectionSets.Count == 1)
        {
            if (state.ContainsState(_selectionSets[0]))
            {
                await base.OnExecuteNodesAsync(context, state, cancellationToken);
            }
        }
        else
        {
            if (_selectionSets.Any(s => state.ContainsState(s)))
            {
                await base.OnExecuteNodesAsync(context, state, cancellationToken);
            }
        }
    }

    protected override void FormatProperties(Utf8JsonWriter writer)
    {
        writer.WritePropertyName("selectionSetIds");
        writer.WriteStartArray();

        foreach (var selectionSet in _selectionSets)
        {
            writer.WriteNumberValue(selectionSet.Id);
        }

        writer.WriteEndArray();
    }
}
