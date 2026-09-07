using System.Runtime.InteropServices;
using HotChocolate.Execution.Internal;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing.Tasks;

internal sealed partial class ResolverTask
{
    private async ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            using (DiagnosticEvents.ResolveFieldValue(_context))
            {
                var success = await TryExecuteAsync(cancellationToken).ConfigureAwait(false);
                var nulledPath = CompleteValue(success, cancellationToken);

                if (_streamEnumerator is not null)
                {
                    RegisterStream();
                }

                switch (_taskBuffer.Count)
                {
                    case 0:
                        break;

                    case 1:
                        _operationContext.Scheduler.Register(_taskBuffer[0]);
                        break;

                    default:
                        _operationContext.Scheduler.Register(
                            CollectionsMarshal.AsSpan(_taskBuffer));
                        break;
                }

                if (nulledPath is not null && !cancellationToken.IsCancellationRequested)
                {
                    // the propagated null removed the result data that pending deferred or
                    // streamed branches were rooted in, so those branches are aborted.
                    await AbortBranchesAsync(nulledPath).ConfigureAwait(false);
                }
            }

            Status = _completionStatus;
        }
        catch
        {
            // If an exception occurs on this level it means that something was wrong with the
            // operation context.

            // In this case we will mark the task as faulted and set the result to null.

            // However, we will not report or rethrow the exception since the context was already
            // destroyed, and we would cause further exceptions.

            // The exception on this level is most likely caused by a cancellation of the request.
            Status = ExecutionTaskStatus.Faulted;
            _context.Result = null;
        }
        finally
        {
            try
            {
                // if the stream was not handed over to a stream task we own the enumerator.
                if (_streamEnumerator is { } enumerator)
                {
                    _streamEnumerator = null;
                    await enumerator.DisposeAsync().ConfigureAwait(false);
                }

                if (_context.HasCleanupTasks)
                {
                    await _context.ExecuteCleanupTasksAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                _operationContext.Scheduler.Complete(this);
                objectPool.Return(this);
            }
        }
    }

    private async ValueTask<bool> TryExecuteAsync(CancellationToken cancellationToken)
    {
        // We will pre-check if the request was already canceled and mark the task as faulted if
        // this is the case. This essentially gives us a cheap and easy way out without any
        // exceptions.
        if (cancellationToken.IsCancellationRequested)
        {
            _completionStatus = ExecutionTaskStatus.Faulted;
            return false;
        }

        try
        {
            // If the arguments are already parsed and processed we can just process.
            // Arguments need no pre-processing if they have no variables.
            if (Selection.Arguments.IsFullyCoercedNoErrors)
            {
                _context.Arguments = Selection.Arguments;
                await ExecuteResolverPipelineAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }

            // if we have errors on the compiled execution plan we will report the errors and
            // signal that this resolver task has errors and shall end.
            if (Selection.Arguments.HasErrors)
            {
                foreach (var argument in Selection.Arguments.ArgumentValues)
                {
                    if (argument.HasError)
                    {
                        _context.ReportError(argument.Error!);
                    }
                }

                return false;
            }

            // if this field has arguments that contain variables we first need to coerce them
            // before we can start executing the resolver.
            // We coerce on the args dictionary that is pooled together with this task.
            Selection.Arguments.CoerceArguments(_context.Variables, _args);
            _context.Arguments = _args;
            await ExecuteResolverPipelineAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                // If cancellation has not been requested for the request we assume this to
                // be a GraphQL resolver error and report it as such.
                // This will let the error handler produce a GraphQL error, and
                // we set the result to null.
                Context.ReportError(ex);
                Context.Result = null;
            }
        }

        return false;
    }

    private async ValueTask ExecuteResolverPipelineAsync(CancellationToken cancellationToken)
    {
        if (_context.Field.DependencyInjectionScope == DependencyInjectionScope.Resolver)
        {
            var serviceScope = _operationContext.Services.CreateAsyncScope();
            _context.Services = serviceScope.ServiceProvider;
            _context.RegisterForCleanup(serviceScope.DisposeAsync);
            _operationContext.ServiceScopeInitializer.Initialize(
                _context, _context.RequestServices, _context.Services);
        }

        await _context.ResolverPipeline!(_context).ConfigureAwait(false);

        var result = _context.Result;

        if (result is null)
        {
            return;
        }

        if (result is IError error)
        {
            _context.ReportError(error);
            _context.Result = null;
            return;
        }

        if (_selection.Field.ResultPostProcessor is not { } postProcessor)
        {
            return;
        }

        // The stream flag is set at compile time on statically streamable fields; the directive
        // arguments can still depend on variables and are therefore resolved here.
        if (_selection.IsStream && TryGetStreamArguments(out var initialCount, out var label))
        {
            _context.Result =
                await CreateStreamResultAsync(postProcessor, result, initialCount, label, cancellationToken)
                    .ConfigureAwait(false);
            return;
        }

        _context.Result = await postProcessor.ToCompletionResultAsync(result, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Completes the initial slice of a streamed list inline and takes ownership of the
    /// enumerator when the source has more items than the initial slice.
    /// </summary>
    private async ValueTask<object?> CreateStreamResultAsync(
        IResolverResultPostProcessor postProcessor,
        object result,
        int initialCount,
        string? label,
        CancellationToken cancellationToken)
    {
        var stream = postProcessor.ToStreamResultAsync(result, cancellationToken);
        var enumerator = stream.GetAsyncEnumerator(cancellationToken);
        var items = new List<object?>();
        var hasMoreItems = true;

        try
        {
            // We read one item beyond the initial slice. If the source is exhausted at or before
            // the initial count the whole list is completed inline and no stream is registered.
            while (items.Count <= initialCount)
            {
                if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    hasMoreItems = false;
                    break;
                }

                items.Add(enumerator.Current);
            }
        }
        catch
        {
            await enumerator.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        if (!hasMoreItems)
        {
            await enumerator.DisposeAsync().ConfigureAwait(false);
            return items;
        }

        // the look-ahead item is not part of the initial slice and is handed
        // over to the stream task together with the enumerator.
        _streamLookAheadItem = items[^1];
        items.RemoveAt(items.Count - 1);
        _streamEnumerator = enumerator;
        _streamLabel = label;
        _streamNextIndex = items.Count;

        return items;
    }

    /// <summary>
    /// Registers the stream branch and hands the enumerator plus the resolver cleanup tasks
    /// over to a stream task.
    /// </summary>
    private void RegisterStream()
    {
        var enumerator = _streamEnumerator!;

        if (_completionStatus is not ExecutionTaskStatus.Completed
            || _context.ResultValue.IsNullOrInvalidated)
        {
            // the streamed field did not complete, so there is nothing to append to.
            return;
        }

        _streamEnumerator = null;

        var path = _context.Path;
        var branchId = _operationContext.DeferExecutionCoordinator
            .RegisterStreamBranch(BranchId, path, _streamLabel);

        _taskBuffer.Add(
            _operationContext.CreateStreamTask(
                _context.Parent<object?>(),
                _selection,
                path,
                _context.ScopedContextData,
                DeferUsage,
                branchId,
                enumerator,
                _streamLookAheadItem,
                _streamNextIndex,
                _context.DetachCleanupTasks()));

        _streamLookAheadItem = null;
    }

    /// <summary>
    /// Resolves the arguments of the stream directive and returns <c>false</c>
    /// when the stream is disabled through its if argument.
    /// </summary>
    private bool TryGetStreamArguments(out int initialCount, out string? label)
    {
        initialCount = 0;
        label = null;

        var directive = _selection.SyntaxNodes[0].Node.GetStreamDirective();

        if (directive is null)
        {
            return false;
        }

        var variables = _context.Variables;

        for (var i = 0; i < directive.Arguments.Count; i++)
        {
            var argument = directive.Arguments[i];

            switch (argument.Name.Value)
            {
                case DirectiveNames.Stream.Arguments.If:
                    if (!GetBooleanValue(variables, argument.Value, defaultValue: true))
                    {
                        return false;
                    }
                    break;

                case DirectiveNames.Stream.Arguments.InitialCount:
                    initialCount = Math.Max(GetIntValue(variables, argument.Value, defaultValue: 0), 0);
                    break;

                case DirectiveNames.Stream.Arguments.Label:
                    label = GetStringValue(variables, argument.Value);
                    break;
            }
        }

        return true;
    }

    private static bool GetBooleanValue(
        IVariableValueCollection variables,
        IValueNode value,
        bool defaultValue)
    {
        if (value is VariableNode variable)
        {
            return variables.TryGetValue<BooleanValueNode>(variable.Name.Value, out var variableValue)
                ? variableValue.Value
                : defaultValue;
        }

        return value is BooleanValueNode booleanValue ? booleanValue.Value : defaultValue;
    }

    private static int GetIntValue(
        IVariableValueCollection variables,
        IValueNode value,
        int defaultValue)
    {
        if (value is VariableNode variable)
        {
            return variables.TryGetValue<IntValueNode>(variable.Name.Value, out var variableValue)
                ? variableValue.ToInt32()
                : defaultValue;
        }

        return value is IntValueNode intValue ? intValue.ToInt32() : defaultValue;
    }

    private static string? GetStringValue(
        IVariableValueCollection variables,
        IValueNode value)
    {
        if (value is VariableNode variable)
        {
            return variables.TryGetValue<StringValueNode>(variable.Name.Value, out var variableValue)
                ? variableValue.Value
                : null;
        }

        return value is StringValueNode stringValue ? stringValue.Value : null;
    }

    /// <summary>
    /// <para>
    /// In most cases a resolver task is rented and returned to its pool after execution.
    /// The execute method itself will return the task.
    /// </para>
    /// <para>
    /// But there are a couple of edge cases where we rent a dummy task and do not execute it.
    /// In these we do want to return it manually.
    /// </para>
    /// <para>Caution: This method is unsafe and could lead to double returns to the pool.</para>
    /// </summary>
    public async ValueTask CompleteUnsafeAsync()
    {
        if (!this.IsCompleted())
        {
            if (_context.HasCleanupTasks)
            {
                await _context.ExecuteCleanupTasksAsync().ConfigureAwait(false);
            }

            Status = _completionStatus;
            _operationContext.Scheduler.Complete(this);
            objectPool.Return(this);
        }
    }
}
