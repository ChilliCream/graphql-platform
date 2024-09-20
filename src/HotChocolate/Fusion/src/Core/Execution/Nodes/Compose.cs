using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using HotChocolate.Execution.Processing;
using static HotChocolate.Fusion.Execution.ExecutionUtils;
using static HotChocolate.Fusion.Utilities.Utf8QueryPlanPropertyNames;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// The <see cref="Compose"/> node composes the results of multiple selection sets.
/// </summary>
internal sealed class Compose : QueryPlanNode
{
    private readonly SelectionSet[] _selectionSets;

    /// <summary>
    /// Initializes a new instance of <see cref="Compose"/>.
    /// </summary>
    /// <param name="id">
    /// The unique id of this node.
    /// <remarks>
    /// Unique withing its query plan.
    /// </remarks>
    /// </param>
    /// <param name="selectionSet">
    /// The selection set for which the results shall be composed.
    /// </param>
    public Compose(int id, SelectionSet selectionSet)
        : this(id, [selectionSet])
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Compose"/>.
    /// </summary>
    /// <param name="id">
    /// The unique id of this node.
    /// <remarks>
    /// Unique withing its query plan.
    /// </remarks>
    /// </param>
    /// <param name="selectionSets">
    /// The selection sets for which the results shall be composed.
    /// </param>
    public Compose(int id, IEnumerable<SelectionSet> selectionSets) : base(id)
    {
        ArgumentNullException.ThrowIfNull(selectionSets);
        _selectionSets = selectionSets.Distinct().OrderBy(t => t.Id).ToArray();
    }

    /// <summary>
    /// Gets the kind of this node.
    /// </summary>
    public override QueryPlanNodeKind Kind => QueryPlanNodeKind.Compose;

    /// <summary>
    /// Composes the results for the associated selection sets.
    /// </summary>
    /// <param name="context">
    /// The execution context.
    /// </param>
    /// <param name="state">
    /// The GraphQL request state.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns the task representing the execution state.
    /// </returns>
    protected override Task OnExecuteAsync(
        FusionExecutionContext context,
        RequestState state,
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

    /// <summary>
    /// Executes the child nodes of this node.
    /// </summary>
    /// <param name="context">
    /// The execution context.
    /// </param>
    /// <param name="state">
    /// The GraphQL request state.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns the task representing the execution state.
    /// </returns>
    protected override async Task OnExecuteNodesAsync(
        FusionExecutionContext context,
        RequestState state,
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

    /// <summary>
    /// Formats this node to JSON.
    /// </summary>
    /// <param name="writer">
    /// The JSON writer.
    /// </param>
    protected override void FormatProperties(Utf8JsonWriter writer)
    {
        writer.WritePropertyName(SelectionSetIdsProp);
        writer.WriteStartArray();

        ref var selectionSet = ref MemoryMarshal.GetArrayDataReference(_selectionSets);
        ref var end = ref Unsafe.Add(ref selectionSet, _selectionSets.Length);

        while (Unsafe.IsAddressLessThan(ref selectionSet, ref end))
        {
            writer.WriteNumberValue(selectionSet.Id);
            selectionSet = ref Unsafe.Add(ref selectionSet, 1)!;
        }

        writer.WriteEndArray();
    }
}
