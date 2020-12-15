using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing
{
    internal sealed partial class ResolverTask : IExecutionTask
    {
        private readonly MiddlewareContext _context = new MiddlewareContext();
        private IOperationContext _operationContext = default!;
        private ISelection _selection = default!;
        private ValueTask _task;

        public bool IsCompleted => _task.IsCompleted;

        public bool IsCanceled { get; private set; }

        public void BeginExecute(CancellationToken cancellationToken)
        {
            _operationContext.Execution.TaskStats.TaskStarted();
            _task = TryExecuteAndCompleteAsync(cancellationToken);
        }

        private async ValueTask TryExecuteAndCompleteAsync(CancellationToken cancellationToken)
        {
            try
            {
                using (_operationContext.DiagnosticEvents.ResolveFieldValue(_context))
                {
                    var success = await TryExecuteAsync(cancellationToken).ConfigureAwait(false);
                    CompleteValue(success, cancellationToken);
                }
            }
            catch
            {
                IsCanceled = true;

                // we suppress any exception if the cancellation was requested.
                if (!cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
            }
            finally
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    IsCanceled = true;
                }
                else
                {
                    _operationContext.Execution.TaskStats.TaskCompleted();
                }

                _operationContext.Execution.TaskPool.Return(this);
            }
        }

        private async ValueTask<bool> TryExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!cancellationToken.IsCancellationRequested &&
                    _selection.Arguments.TryCoerceArguments(
                    _context.Variables,
                    _context.ReportError,
                    out IReadOnlyDictionary<NameString, ArgumentValue>? coercedArgs))
                {
                    _context.Arguments = coercedArgs;
                    await ExecuteResolverPipelineAsync(cancellationToken).ConfigureAwait(false);
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    _context.ReportError(ex);
                    _context.Result = null;
                }
            }

            return false;
        }

        private async ValueTask ExecuteResolverPipelineAsync(CancellationToken cancellationToken)
        {
            await _context.ResolverPipeline(_context).ConfigureAwait(false);

            switch (_context.Result)
            {
                case IExecutable executable:
                    _context.Result = await executable
                        .ToListAsync(cancellationToken)
                        .ConfigureAwait(false);
                    break;

                case IQueryable queryable:
                    _context.Result = await Task.Run(() =>
                    {
                        var items = new List<object?>();
                        foreach (object? o in queryable)
                        {
                            items.Add(o);

                            if (cancellationToken.IsCancellationRequested)
                            {
                                break;
                            }
                        }
                        return items;
                    }, cancellationToken);
                    break;

                case IError error:
                    _context.ReportError(error);
                    _context.Result = null;
                    break;

                case IEnumerable<IError> errors:
                    foreach (IError error in errors)
                    {
                        _context.ReportError(error);
                    }
                    _context.Result = null;
                    break;
            }
        }

        private void CompleteValue(bool success, CancellationToken cancellationToken)
        {
            object? completedValue = null;

            try
            {
                // we will only try to complete the resolver value if there are no known errors.
                if (success)
                {
                    if (ValueCompletion.TryComplete(
                        _operationContext,
                        _context,
                        _context.Path,
                        _context.Field.Type,
                        _context.Result,
                        out completedValue) &&
                        !_context.Field.Type.IsLeafType() &&
                        completedValue is IHasResultDataParent result)
                    {
                        result.Parent = _context.ResultMap;
                    }
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
                if (cancellationToken.IsCancellationRequested)
                {
                    // if cancellation is request we do no longer report errors to the
                    // operation context.
                    return;
                }

                _context.ReportError(ex);
                _context.Result = null;
            }

            if (completedValue is null && _context.Field.Type.IsNonNullType())
            {
                // if we detect a non-null violation we will stash it for later.
                // the non-null propagation is delayed so that we can parallelize better.
                _operationContext.Result.AddNonNullViolation(
                    _context.FieldSelection,
                    _context.Path,
                    _context.ResultMap);
            }
            else
            {
                _context.ResultMap.SetValue(
                    _context.ResponseIndex,
                    _context.ResponseName,
                    completedValue,
                    _context.Field.Type.IsNullableType());
            }
        }
    }
}
