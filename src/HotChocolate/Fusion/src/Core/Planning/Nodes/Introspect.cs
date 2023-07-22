using System.Collections.Immutable;
using System.Text.Json;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Execution;
using HotChocolate.Language;
using static HotChocolate.Fusion.Planning.Utf8QueryPlanPropertyNames;

namespace HotChocolate.Fusion.Planning;

internal sealed class Introspect : QueryPlanNode
{
    private readonly SelectionSet _selectionSet;

    public Introspect(int id, SelectionSet selectionSet) : base(id)
    {
        _selectionSet = selectionSet ?? throw new ArgumentNullException(nameof(selectionSet));
    }

    public override QueryPlanNodeKind Kind => QueryPlanNodeKind.Introspect;

    protected override async Task OnExecuteAsync(
        FusionExecutionContext context,
        RequestState state,
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
        var rootSelections = _selectionSet.Selections;

        for (var i = 0; i < rootSelections.Count; i++)
        {
            var selection = rootSelections[i];
            if (selection.Field.IsIntrospectionField)
            {
                rootSelectionNodes.Add(rootSelections[i].SyntaxNode);
            }
        }

        var selectionSetNode = new SelectionSetNode(null, rootSelectionNodes);
        writer.WriteString(DocumentProp, selectionSetNode.ToString(false));
    }
}
