using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Execution;
using HotChocolate.Language;
using static HotChocolate.Fusion.Planning.Utf8QueryPlanPropertyNames;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// The <see cref="Introspect"/> node is responsible for executing introspection selection of the GraphQL request.
/// </summary>
internal sealed class Introspect : QueryPlanNode
{
    private readonly SelectionSet _selectionSet;

    /// <summary>
    /// Initializes a new instance of <see cref="Introspect"/>.
    /// </summary>
    /// <param name="id">
    /// The unique id of this node.
    /// </param>
    /// <param name="selectionSet">
    /// The selection set for which the results shall be composed.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="selectionSet"/> is <c>null</c>.
    /// </exception>
    public Introspect(int id, SelectionSet selectionSet) : base(id)
    {
        _selectionSet = selectionSet ?? throw new ArgumentNullException(nameof(selectionSet));
    }

    /// <summary>
    /// Gets the kind of this node.
    /// </summary>
    public override QueryPlanNodeKind Kind => QueryPlanNodeKind.Introspect;

    /// <summary>
    /// Executes the introspection selections of the GraphQL request.
    /// </summary>
    /// <param name="context">
    /// The execution context.
    /// </param>
    /// <param name="state">
    /// The execution state.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    protected override async Task OnExecuteAsync(
        FusionExecutionContext context,
        RequestState state,
        CancellationToken cancellationToken)
    {
        if (state.TryGetState(_selectionSet, out var values))
        {
            var value = values[0];
            var operationContext = context.OperationContext;
            var rootSelections = _selectionSet.Selections;

            for (var i = 0; i < rootSelections.Count; i++)
            {
                var selection = rootSelections[i];

                if (selection.Field.IsIntrospectionField)
                {
                    var resolverTask = operationContext.CreateResolverTask(
                        selection,
                        operationContext.RootValue,
                        value.SelectionSetResult,
                        i,
                        operationContext.PathFactory.Append(Path.Root, selection.ResponseName),
                        ImmutableDictionary<string, object?>.Empty);
                    resolverTask.BeginExecute(cancellationToken);

                    await resolverTask.WaitForCompletionAsync(cancellationToken);
                }
            }
        }
    }

    protected override void FormatProperties(Utf8JsonWriter writer)
    {
        var rootSelectionNodes = new List<ISelectionNode>();
        var rootSelectionSet = Unsafe.As<SelectionSet>(_selectionSet);
        ref var selection = ref rootSelectionSet.GetSelectionsReference();
        ref var end = ref Unsafe.Add(ref selection, rootSelectionSet.Selections.Count);

        while (Unsafe.IsAddressLessThan(ref selection, ref end))
        {
            if (selection.Field.IsIntrospectionField)
            {
                rootSelectionNodes.Add(selection.SyntaxNode);
            }
            
            selection = ref Unsafe.Add(ref selection, 1)!;
        }

        var selectionSetNode = new SelectionSetNode(null, rootSelectionNodes);
        writer.WriteString(DocumentProp, selectionSetNode.ToString(false));
    }
}
