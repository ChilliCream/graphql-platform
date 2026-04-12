using System.Text.Json;
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

    protected override async ValueTask<ExecutionStatus> OnExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
    {
        var backlog = new Stack<(object? Parent, Selection Selection, SourceResultElementBuilder Result)>();
        var resultBuilder = new SourceResultDocumentBuilder(context.OperationPlan.Operation, context.IncludeFlags);
        var root = resultBuilder.Root;
        var index = 0;

        foreach (var selection in _selections)
        {
            if ((selection.Resolver is null && selection.AsyncResolver is null)
                || !selection.Field.IsIntrospectionField
                || !selection.IsIncluded(context.IncludeFlags))
            {
                continue;
            }

            var property = root.CreateProperty(selection, index++);
            backlog.Push((null, selection, property));
        }

        await ExecuteSelectionsAsync(context, backlog, cancellationToken).ConfigureAwait(false);
        context.AddPartialResults(resultBuilder.Build(), _resultSelectionSet);

        return ExecutionStatus.Success;
    }

    protected override IDisposable CreateScope(OperationPlanContext context)
        => context.DiagnosticEvents.ExecuteIntrospectionNode(context, this);

    private static async ValueTask ExecuteSelectionsAsync(
        OperationPlanContext context,
        Stack<(object? Parent, Selection Selection, SourceResultElementBuilder Result)> backlog,
        CancellationToken cancellationToken)
    {
        var operation = context.OperationPlan.Operation;
        var fieldContext = new ReusableFieldContext(
            context.Schema,
            context.Variables,
            context.IncludeFlags,
            context.CreateRentedBuffer(),
            cancellationToken);

        while (backlog.TryPop(out var current))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (parent, selection, result) = current;
            fieldContext.Initialize(parent, selection, result);

            if (selection.AsyncResolver is { } asyncResolver)
            {
                await asyncResolver.Invoke(fieldContext).ConfigureAwait(false);
            }
            else if (selection.Resolver is { } resolver)
            {
                resolver.Invoke(fieldContext);
            }
            else
            {
                throw new InvalidOperationException(
                    $"No resolver found for selection '{selection.ResponseName}' "
                    + $"on field '{selection.Field.Name}'.");
            }

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

        // For abstract types (unions), determine the concrete introspection type
        // from the runtime result.
        var typeName = runtimeResult switch
        {
            ITypeDefinition => "__Type",
            IOutputFieldDefinition => "__Field",
            IInputValueDefinition => "__InputValue",
            IEnumValue => "__EnumValue",
            IDirectiveDefinition => "__Directive",
            _ => throw new InvalidOperationException(
                $"Cannot determine the concrete object type for abstract type '{namedType}'"
                + $" from runtime result of type '{runtimeResult?.GetType().Name}'.")
        };

        if (schema.Types.TryGetType(typeName, out var resolvedType)
            && resolvedType is IObjectTypeDefinition resolvedObjectType)
        {
            return resolvedObjectType;
        }

        throw new InvalidOperationException(
            $"Introspection type '{typeName}' not found in schema.");
    }
}
