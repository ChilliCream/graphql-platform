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

        // The planner feeds this node from two paths: an introspection-only operation
        // passes the whole root selection set, a mixed one passes only the introspection
        // selections. Both are narrowed here, once, to what this node can actually
        // resolve. Everything left varies per request only through the include flags,
        // which the result document builder applies on its own.
        _selections = FilterResolvableSelections(selections);

        // The result selection set stays over the selections as they were handed in.
        // It backs error pocketing, where a wider set is safe and a narrower one is not.
        var selectionSetNode = new SelectionSetNode(selections.Select(t => t.SyntaxNodes[0].Node).ToArray());
        _resultSelectionSet = ResultSelectionSet.Create(selectionSetNode);
        _conditions = conditions;
    }

    private static Selection[] FilterResolvableSelections(Selection[] selections)
    {
        var resolvable = new Selection[selections.Length];
        var count = 0;

        foreach (var selection in selections)
        {
            if ((selection.Resolver is null && selection.AsyncResolver is null)
                || !selection.Field.IsIntrospectionField)
            {
                continue;
            }

            resolvable[count++] = selection;
        }

        return count == selections.Length ? selections : resolvable[..count];
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

        // The document is shaped from exactly the selections this node resolves. The
        // builder drops the ones this request excludes and stamps each remaining slot
        // with its selection, so enumerating the slots back is what keeps the document
        // and this node's work in step.
        var resultBuilder = new SourceResultDocumentBuilder(
            context.Memory,
            context.OperationPlan.Operation,
            context.IncludeFlags,
            _selections);

        foreach (var (selection, property) in resultBuilder.Root.EnumerateProperties())
        {
            backlog.Push((null, selection, property));
        }

        try
        {
            await ExecuteSelectionsAsync(context, backlog, cancellationToken).ConfigureAwait(false);

            context.AddPartialResults(resultBuilder.Build(), _resultSelectionSet);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return ExecutionStatus.Failed;
        }
        catch (GraphQLException ex)
        {
            foreach (var error in ex.Errors)
            {
                context.AddErrors(error, _resultSelectionSet, Path.Root);
            }

            return ExecutionStatus.Failed;
        }
        catch (Exception ex)
        {
            var error = ErrorBuilder.FromException(ex).Build();
            context.AddErrors(error, _resultSelectionSet, Path.Root);

            return ExecutionStatus.Failed;
        }

        return ExecutionStatus.Success;
    }

    protected override IDisposable CreateScope(OperationPlanContext context)
        => context.DiagnosticEvents.ExecuteIntrospectionNode(context, this);

    private static async ValueTask ExecuteSelectionsAsync(
        OperationPlanContext context,
        Stack<(object? Parent, Selection Selection, SourceResultElementBuilder Result)> backlog,
        CancellationToken cancellationToken)
    {
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

                // The resolver shaped these objects through CreateObjectValue, which
                // resolved the selection set and applied the include flags itself. The
                // slots it laid out are the authority on what still has to be executed,
                // so they are read back rather than derived a second time here.
                if (result.ValueKind is JsonValueKind.Object
                    && (namedType.IsObjectType() || namedType.IsAbstractType()))
                {
                    foreach (var (childSelection, property) in result.EnumerateProperties())
                    {
                        backlog.Push((fieldContext.RuntimeResults[0], childSelection, property));
                    }
                }
                else if (result.ValueKind is JsonValueKind.Array
                    && selection.Type.IsListType()
                    && (namedType.IsObjectType() || namedType.IsAbstractType()))
                {
                    var i = 0;
                    foreach (var element in result.EnumerateArray())
                    {
                        var runtimeResult = fieldContext.RuntimeResults[i++];

                        if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                        {
                            continue;
                        }

                        foreach (var (childSelection, property) in element.EnumerateProperties())
                        {
                            backlog.Push((runtimeResult, childSelection, property));
                        }
                    }
                }
            }
        }
    }
}
