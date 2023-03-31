using System.Collections.Immutable;
using System.Text.Json;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Execution;
using HotChocolate.Language;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Planning;

internal sealed class Introspect : QueryPlanNode
{
    private readonly ISelectionSet _selectionSet;

    public Introspect(int id, ISelectionSet selectionSet) : base(id)
    {
        _selectionSet = selectionSet ?? throw new ArgumentNullException(nameof(selectionSet));
    }

    public override QueryPlanNodeKind Kind => QueryPlanNodeKind.Introspect;

    protected override async Task OnExecuteAsync(
        FusionExecutionContext context,
        ExecutionState state,
        CancellationToken cancellationToken)
    {
        if (state.TryGetState(_selectionSet, out var values))
        {
            var operationContext = context.OperationContext;
            var rootSelections = _selectionSet.Selections;
            var value = values.Single();

            for (var i = 0; i < rootSelections.Count; i++)
            {
                var selection = rootSelections[i];

                if (selection.Field.IsIntrospectionField)
                {
                    var resolverTask = operationContext.CreateResolverTask(
                        selection,
                        operationContext.RootValue,
                        value.Result,
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
        var rootSelections = _selectionSet.Selections;

        for (var i = 0; i < rootSelections.Count; i++)
        {
            var selection = rootSelections[i];
            if (selection.Field.IsIntrospectionField &&
                !selection.Field.Name.EqualsOrdinal(IntrospectionFields.TypeName))
            {
                rootSelectionNodes.Add(rootSelections[i].SyntaxNode);
            }
        }

        var selectionSetNode = new SelectionSetNode(null, rootSelectionNodes);
        writer.WriteString("document", selectionSetNode.ToString(false));
    }
}
