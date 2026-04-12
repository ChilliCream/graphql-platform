using System.Text.Json;
using HotChocolate.Fusion.Execution.Introspection;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class IntrospectionExecutionNode : ExecutionNode
{
    private readonly Selection[] _selections;
    private readonly ResultSelectionSet _resultSelectionSet;
    private readonly ExecutionNodeCondition[] _conditions;

    public IntrospectionExecutionNode(
        int id,
        Selection[] selections,
        ExecutionNodeCondition[] conditions)
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
        var selectionSetNode = new SelectionSetNode(selections.Select(t => t.SyntaxNodes[0].Node).ToArray());
        _resultSelectionSet = ResultSelectionSet.Create(selectionSetNode);
        _conditions = conditions;
    }

    /// <inheritdoc />
    public override int Id { get; }

    /// <inheritdoc />
    public override ExecutionNodeType Type => ExecutionNodeType.Introspection;

    /// <inheritdoc />
    public override ReadOnlySpan<ExecutionNodeCondition> Conditions => _conditions;

    /// <inheritdoc />
    public override string? SchemaName => null;

    /// <summary>
    /// The introspection selections.
    /// </summary>
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
        context.AddPartialResults(resultBuilder.Build(), _resultSelectionSet);

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
                var namedType = selection.Type.NamedType();

                if (result.ValueKind is JsonValueKind.Object
                    && (namedType.IsObjectType() || namedType.IsAbstractType()))
                {
                    var objectType = ResolveObjectType(
                        namedType,
                        fieldContext.RuntimeResults[0],
                        context.Schema);
                    var selectionSet = operation.GetSelectionSet(selection, objectType);

                    var j = 0;
                    for (var i = 0; i < selectionSet.Selections.Length; i++)
                    {
                        var childSelection = selectionSet.Selections[i];

                        if (!childSelection.IsIncluded(context.IncludeFlags))
                        {
                            continue;
                        }

                        var property = result.CreateProperty(childSelection, j++);
                        backlog.Push((fieldContext.RuntimeResults[0], childSelection, property));
                    }
                }
                else if (result.ValueKind is JsonValueKind.Array
                    && selection.Type.IsListType()
                    && (namedType.IsObjectType() || namedType.IsAbstractType()))
                {
                    var isAbstract = namedType.IsAbstractType();

                    // For non-abstract list types, resolve the selection set once.
                    SelectionSet? staticSelectionSet = null;
                    if (!isAbstract)
                    {
                        var objectType = namedType as IObjectTypeDefinition
                            ?? selection.Type.NamedType<IObjectTypeDefinition>();
                        staticSelectionSet = operation.GetSelectionSet(selection, objectType);
                    }

                    var i = 0;
                    foreach (var element in result.EnumerateArray())
                    {
                        var runtimeResult = fieldContext.RuntimeResults[i++];

                        if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                        {
                            continue;
                        }

                        var selectionSet = staticSelectionSet
                            ?? operation.GetSelectionSet(
                                selection,
                                ResolveObjectType(namedType, runtimeResult, context.Schema));

                        var k = 0;
                        for (var j = 0; j < selectionSet.Selections.Length; j++)
                        {
                            var childSelection = selectionSet.Selections[j];

                            if (!childSelection.IsIncluded(context.IncludeFlags))
                            {
                                continue;
                            }

                            var property = element.CreateProperty(childSelection, k++);
                            backlog.Push((runtimeResult, childSelection, property));
                        }
                    }
                }
            }
        }
    }

    private static IObjectTypeDefinition ResolveObjectType(
        IType namedType,
        object? runtimeResult,
        ISchemaDefinition schema)
    {
        if (namedType is IObjectTypeDefinition objectType)
        {
            return objectType;
        }

        // For abstract types, determine the concrete type from the runtime result.
        var typeName = SchemaCoordinateResolver.GetTypeName(runtimeResult!);

        if (typeName is not null
            && schema.Types.TryGetType(typeName, out var resolvedType)
            && resolvedType is IObjectTypeDefinition resolvedObjectType)
        {
            return resolvedObjectType;
        }

        throw new InvalidOperationException(
            $"Cannot determine the concrete object type for abstract type '{namedType}'"
            + $" from runtime result of type '{runtimeResult?.GetType().Name}'.");
    }
}
