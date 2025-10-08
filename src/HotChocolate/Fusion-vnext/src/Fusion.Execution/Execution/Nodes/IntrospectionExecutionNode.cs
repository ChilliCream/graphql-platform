using System.Text.Json;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class IntrospectionExecutionNode : ExecutionNode
{
    private readonly Selection[] _selections;
    private readonly string[] _responseNames;

    public IntrospectionExecutionNode(int id, Selection[] selections)
    {
        ArgumentNullException.ThrowIfNull(selections);

        if (selections.Length == 0)
        {
            throw new ArgumentException(
                "There must be at least one introspection selection.",
                nameof(selections));
        }

        Id = id;
        _selections = selections;
        _responseNames = selections.Select(t => t.ResponseName).ToArray();
    }

    public override int Id { get; }

    public override ExecutionNodeType Type => ExecutionNodeType.Introspection;

    public ReadOnlySpan<Selection> Selections => _selections;

    protected override ValueTask<ExecutionStatus> OnExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
    {
        var backlog = new Stack<(object? Parent, Selection Selection, SourceResultElementBuilder Result)>();
        var resultBuilder = new SourceResultDocumentBuilder(context.OperationPlan.Operation, context.IncludeFlags);
        var root = resultBuilder.Root;
        var index = 0;

        foreach (var selection in _selections)
        {
            if (selection.Resolver is null
                || !selection.Field.IsIntrospectionField
                || !selection.IsIncluded(context.IncludeFlags))
            {
                continue;
            }

            var property = root.CreateProperty(selection, index++);
            backlog.Push((null, selection, property));
        }

        ExecuteSelections(context, backlog);
        context.AddPartialResults(resultBuilder.Build(), _responseNames);

        return new ValueTask<ExecutionStatus>(ExecutionStatus.Success);
    }

    protected override IDisposable CreateScope(OperationPlanContext context)
        => context.DiagnosticEvents.ExecuteIntrospectionNode(context, this);

    private static void ExecuteSelections(
        OperationPlanContext context,
        Stack<(object? Parent, Selection Selection, SourceResultElementBuilder Result)> backlog)
    {
        var operation = context.OperationPlan.Operation;
        var fieldContext = new ReusableFieldContext(
            context.Schema,
            context.Variables,
            context.IncludeFlags,
            context.CreateRentedBuffer());

        while (backlog.TryPop(out var current))
        {
            var (parent, selection, result) = current;
            fieldContext.Initialize(parent, selection, result);

            selection.Resolver?.Invoke(fieldContext);

            if (!selection.IsLeaf)
            {
                if (result.ValueKind is JsonValueKind.Object && selection.Type.IsObjectType())
                {
                    var objectType = selection.Type.NamedType<IObjectTypeDefinition>();
                    var selectionSet = operation.GetSelectionSet(selection, objectType);

                    for (var i = 0; i < selectionSet.Selections.Length; i++)
                    {
                        var childSelection = selectionSet.Selections[i];

                        if (!childSelection.IsIncluded(context.IncludeFlags))
                        {
                            continue;
                        }

                        var property = result.CreateProperty(childSelection, i);
                        backlog.Push((fieldContext.RuntimeResults[0], childSelection, property));
                    }
                }
                else if (result.ValueKind is JsonValueKind.Array
                    && selection.Type.IsListType()
                    && selection.Type.NamedType().IsObjectType())
                {
                    var objectType = selection.Type.NamedType<IObjectTypeDefinition>();
                    var selectionSet = operation.GetSelectionSet(selection, objectType);

                    var i = 0;
                    foreach (var element in result.EnumerateArray())
                    {
                        var runtimeResult = fieldContext.RuntimeResults[i++];

                        if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                        {
                            continue;
                        }

                        for (var j = 0; j < selectionSet.Selections.Length; j++)
                        {
                            var childSelection = selectionSet.Selections[j];

                            if (!childSelection.IsIncluded(context.IncludeFlags))
                            {
                                continue;
                            }

                            var property = result.CreateProperty(childSelection, i);
                            backlog.Push((runtimeResult, childSelection, property));
                        }
                    }
                }
            }
        }
    }
}
