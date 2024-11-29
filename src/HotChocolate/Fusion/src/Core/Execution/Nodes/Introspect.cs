using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Fusion.Utilities.Utf8QueryPlanPropertyNames;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// The <see cref="Introspect"/> node is responsible for executing introspection selection of the GraphQL request.
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
internal sealed class Introspect(int id, SelectionSet selectionSet) : QueryPlanNode(id)
{
    private readonly SelectionSet _selectionSet = selectionSet ?? throw new ArgumentNullException(nameof(selectionSet));

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
            List<Task>? asyncTasks = null;
            ExecutePureFieldsAndEnqueueResolvers(context, value, cancellationToken, ref asyncTasks);

            if(asyncTasks is { Count: > 0, })
            {
                await Task.WhenAll(asyncTasks).ConfigureAwait(false);
            }
        }
    }

    private static void ExecutePureFieldsAndEnqueueResolvers(
        FusionExecutionContext context,
        ExecutionState value,
        CancellationToken ct,
        ref List<Task>? asyncTasks)
    {
        var operationContext = context.OperationContext;
        var rootSelectionSet = Unsafe.As<SelectionSet>(context.Operation.RootSelectionSet);
        ref var selection = ref rootSelectionSet.GetSelectionsReference();
        ref var end = ref Unsafe.Add(ref selection, rootSelectionSet.Selections.Count);
        var rootTypeName = selection.DeclaringType.Name;
        var i = 0;

        while (Unsafe.IsAddressLessThan(ref selection, ref end))
        {
            var field = Unsafe.As<ObjectField>(selection.Field);
            var result = value.SelectionSetResult;

            if (!field.IsIntrospectionField)
            {
                goto NEXT;
            }

            if (field.IsTypeNameField)
            {
                // if the request just asks for the __typename field we immediately resolve it without
                // going through the resolver pipeline.
                result.SetValueUnsafe(i, selection.ResponseName, rootTypeName, false);
                goto NEXT;
            }

            // only for proper introspection fields we will execute the resolver pipeline.
            var resolverTask = operationContext.CreateResolverTask(
                selection,
                operationContext.RootValue,
                result,
                i,
                ImmutableDictionary<string, object?>.Empty);
            resolverTask.BeginExecute(ct);

            asyncTasks ??= [];
            asyncTasks.Add(resolverTask.WaitForCompletionAsync(ct));

            NEXT:
            selection = ref Unsafe.Add(ref selection, 1)!;
            i++;
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
