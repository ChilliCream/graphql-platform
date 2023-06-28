using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Execution;
using static HotChocolate.Fusion.Execution.ExecutorUtils;

namespace HotChocolate.Fusion.Planning;

internal sealed class Compose : QueryPlanNode
{
    private readonly ISelectionSet[] _selectionSets;

    public Compose(int id, Resolve resolve)
        : this(id, new[] { resolve.SelectionSet })
    {
    }

    public Compose(int id, ISelectionSet selectionSet)
        : this(id, new[] { selectionSet })
    {
    }

    public Compose(int id, IReadOnlyList<ISelectionSet> selectionSets) : base(id)
    {
        if (selectionSets is null)
        {
            throw new ArgumentNullException(nameof(selectionSets));
        }

        _selectionSets = selectionSets.Distinct().ToArray();
    }

    public override QueryPlanNodeKind Kind => QueryPlanNodeKind.Compose;

    protected override Task OnExecuteAsync(
        FusionExecutionContext context,
        ExecutionState state,
        CancellationToken cancellationToken)
    {
        if (_selectionSets.Length == 1)
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
            ref var start = ref MemoryMarshal.GetArrayDataReference(_selectionSets);
            ref var end = ref Unsafe.Add(ref start, _selectionSets.Length);

            while (Unsafe.IsAddressLessThan(ref start, ref end))
            {
                if (state.TryGetState(start, out var values))
                {
                    for (var i = 0; i < values.Count; i++)
                    {
                        ComposeResult(context, values[i]);
                    }
                }

                start = ref Unsafe.Add(ref start, 1)!;
            }
        }

        return Task.CompletedTask;
    }

    protected override async Task OnExecuteNodesAsync(
        FusionExecutionContext context,
        ExecutionState state,
        CancellationToken cancellationToken)
    {
        if (_selectionSets.Length == 1)
        {
            if (state.ContainsState(_selectionSets[0]))
            {
                await base.OnExecuteNodesAsync(context, state, cancellationToken);
            }
        }
        else
        {
            if (state.ContainsState(_selectionSets))
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
