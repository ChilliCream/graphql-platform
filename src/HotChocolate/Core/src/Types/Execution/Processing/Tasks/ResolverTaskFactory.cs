using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using HotChocolate.Text.Json;
using HotChocolate.Types;
using static HotChocolate.Execution.Processing.ValueCompletion;

namespace HotChocolate.Execution.Processing.Tasks;

internal static class ResolverTaskFactory
{
    private static List<ResolverTask>? s_pooled = [];

    static ResolverTaskFactory() { }

    public static void EnqueueResolverTasks(
        OperationContext operationContext,
        object? parent,
        ResultElement resultValue,
        IImmutableDictionary<string, object?> scopedContext,
        Path path)
    {
        var selectionSet = resultValue.AssertSelectionSet();
        var selections = selectionSet.Selections;

        var scheduler = operationContext.Scheduler;
        var bufferedTasks = Interlocked.Exchange(ref s_pooled, null) ?? [];
        Debug.Assert(bufferedTasks.Count == 0, "The buffer must be clean.");

        try
        {
            // we are iterating reverse so that in the case of a mutation the first
            // synchronous root selection is executed first, since the work scheduler
            // is using two stacks one for parallel work and one for synchronous work.
            // the scheduler tries to schedule new work first.
            // coincidentally we can use that to schedule a mutation so that we honor the spec
            // guarantees while executing efficient.
            var fieldValues = selections.Length == 1
                ? resultValue.EnumerateObject()
                : resultValue.EnumerateObject().Reverse();
            foreach (var field in fieldValues)
            {
                bufferedTasks.Add(
                    operationContext.CreateResolverTask(
                        parent,
                        field.AssertSelection(),
                        field.Value,
                        scopedContext));
            }

            if (bufferedTasks.Count == 0)
            {
                // in the case all root fields are skipped we execute a dummy task in order
                // to not have extra logic for this case.
                scheduler.Register(new NoOpExecutionTask(operationContext));
            }
            else
            {
                scheduler.Register(CollectionsMarshal.AsSpan(bufferedTasks));
            }
        }
        finally
        {
            bufferedTasks.Clear();
            Interlocked.Exchange(ref s_pooled!, bufferedTasks);
        }
    }

    // TODO : remove ? defer?
    /*
    public static ResolverTask EnqueueElementTasks(
        OperationContext operationContext,
        Selection selection,
        object? parent,
        Path path,
        int index,
        IAsyncEnumerator<object?> value,
        IImmutableDictionary<string, object?> scopedContext)
    {
        var parentResult = operationContext.Result.RentObject(1);
        var bufferedTasks = Interlocked.Exchange(ref s_pooled, null) ?? [];
        Debug.Assert(bufferedTasks.Count == 0, "The buffer must be clean.");

        var resolverTask =
            operationContext.CreateResolverTask(
                selection,
                parent,
                parentResult,
                0,
                scopedContext,
                path.Append(index));

        try
        {
            CompleteInline(
                operationContext,
                resolverTask.Context,
                selection,
                selection.Type.ElementType(),
                0,
                parentResult,
                value.Current,
                bufferedTasks);

            // if we have child tasks we need to register them.
            if (bufferedTasks.Count > 0)
            {
                operationContext.Scheduler.Register(CollectionsMarshal.AsSpan(bufferedTasks));
            }
        }
        finally
        {
            bufferedTasks.Clear();
            Interlocked.Exchange(ref s_pooled, bufferedTasks);
        }

        return resolverTask;
    }
    */

    public static void EnqueueOrInlineResolverTasks(
        ValueCompletionContext context,
        SelectionSet selectionSet,
        ObjectType selectionSetType,
        ResultElement resultValue,
        object parent)
    {
        Debug.Assert(selectionSet.Type == selectionSetType);
        Debug.Assert(resultValue.Type?.NamedType()?.IsAssignableFrom(selectionSetType) ?? false);

        var operationContext = context.OperationContext;

        resultValue.SetObjectValue(selectionSet);

        foreach (var field in resultValue.EnumerateObject())
        {
            var selection = field.AssertSelection();

            if (selection.Strategy is SelectionExecutionStrategy.Pure)
            {
                ResolveAndCompleteInline(
                    context,
                    selection,
                    selectionSetType,
                    field.Value,
                    parent);
            }
            else
            {
                context.Tasks.Add(
                    operationContext.CreateResolverTask(
                        parent,
                        selection,
                        field.Value,
                        context.ResolverContext.ScopedContextData));
            }
        }
    }

    private static void ResolveAndCompleteInline(
        ValueCompletionContext context,
        Selection selection,
        ObjectType selectionSetType,
        ResultElement fieldValue,
        object parent)
    {
        var operationContext = context.OperationContext;
        var resolverContext = context.ResolverContext;
        var executedSuccessfully = false;
        object? resolverResult = null;

        try
        {
            // we first try to create a context for our pure resolver.
            // this should actually only fail if we are unable to coerce
            // the field arguments.
            if (resolverContext.TryCreatePureContext(
                selection,
                selectionSetType,
                fieldValue,
                parent,
                out var childContext))
            {
                // if we have a pure context we can execute out pure resolver.
                resolverResult = selection.PureResolver!(childContext);
                executedSuccessfully = true;
            }
        }
        catch (OperationCanceledException)
        {
            // If we run into this exception the request was aborted.
            // In this case we do nothing and just return.
            return;
        }
        catch (Exception ex)
        {
            operationContext.ReportError(ex, resolverContext, selection, fieldValue.Path);
        }

        if (!executedSuccessfully)
        {
            fieldValue.SetNullValue();
        }

        try
        {
            Complete(context, selection, fieldValue, resolverResult);
        }
        catch (OperationCanceledException)
        {
            // If we run into this exception the request was aborted.
            // In this case we do nothing and just return.
            return;
        }
        catch (Exception ex)
        {
            operationContext.ReportError(ex, resolverContext, selection, fieldValue.Path);
        }

        if (fieldValue is { IsNullable: false, IsNullOrInvalidated: true })
        {
            PropagateNullValues(fieldValue);
            operationContext.Result.AddNonNullViolation(fieldValue.Path);
        }
    }

    private sealed class NoOpExecutionTask(OperationContext context) : ExecutionTask
    {
        protected override IExecutionTaskContext Context { get; } = context;

        protected override ValueTask ExecuteAsync(CancellationToken cancellationToken)
            => default;
    }
}
