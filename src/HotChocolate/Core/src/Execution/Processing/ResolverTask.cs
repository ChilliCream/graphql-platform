using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Types;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing
{
    internal sealed class ResolverTask : ResolverTaskBase
    {
        private readonly ObjectPool<ResolverTask> _objectPool;

        public ResolverTask(ObjectPool<ResolverTask> objectPool)
        {
            _objectPool = objectPool ?? throw new ArgumentNullException(nameof(objectPool));
        }

        public override void BeginExecute(CancellationToken cancellationToken)
        {
            OperationContext.Execution.TaskStats.TaskStarted();
#pragma warning disable 4014
            ExecuteAsync(cancellationToken);
#pragma warning restore 4014
        }

        private async ValueTask ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                using (DiagnosticEvents.ResolveFieldValue(ResolverContext))
                {
                    var success = await TryExecuteAsync(cancellationToken).ConfigureAwait(false);
                    CompleteValue(success, cancellationToken);
                }
            }
            catch
            {
                // we suppress any exception if the cancellation was requested.
                if (!cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
            }
            finally
            {
                OperationContext.Execution.TaskStats.TaskCompleted();
                _objectPool.Return(this);
            }
        }

        private async ValueTask<bool> TryExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!cancellationToken.IsCancellationRequested &&
                    Selection.Arguments.TryCoerceArguments(
                    ResolverContext.Variables,
                    ResolverContext.ReportError,
                    out IReadOnlyDictionary<NameString, ArgumentValue>? coercedArgs))
                {
                    ResolverContext.Arguments = coercedArgs;
                    await ExecuteResolverPipelineAsync(cancellationToken).ConfigureAwait(false);
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    ResolverContext.ReportError(ex);
                    ResolverContext.Result = null;
                }
            }

            return false;
        }

        private async ValueTask ExecuteResolverPipelineAsync(CancellationToken cancellationToken)
        {
            await ResolverContext.ResolverPipeline!(ResolverContext).ConfigureAwait(false);

            switch (ResolverContext.Result)
            {
                case IExecutable executable:
                    ResolverContext.Result = await executable
                        .ToListAsync(cancellationToken)
                        .ConfigureAwait(false);
                    break;

                case IQueryable queryable:
                    ResolverContext.Result = await Task.Run(() =>
                    {
                        var items = new List<object?>();
                        foreach (var o in queryable)
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
                    ResolverContext.ReportError(error);
                    ResolverContext.Result = null;
                    break;

                case IEnumerable<IError> errors:
                    foreach (IError error in errors)
                    {
                        ResolverContext.ReportError(error);
                    }
                    ResolverContext.Result = null;
                    break;
            }
        }
    }

    internal sealed class PureResolverTask : ResolverTaskBase
    {
        private readonly ObjectPool<PureResolverTask> _objectPool;

        public PureResolverTask(ObjectPool<PureResolverTask> objectPool)
        {
            _objectPool = objectPool ?? throw new ArgumentNullException(nameof(objectPool));
        }

        public override void BeginExecute(CancellationToken cancellationToken)
        {
            Execute(cancellationToken);
        }

        private void Execute(CancellationToken cancellationToken)
        {
            try
            {
                using (DiagnosticEvents.ResolveFieldValue(ResolverContext))
                {
                    var success = TryExecute(cancellationToken);
                    CompleteValue(success, cancellationToken);
                }
            }
            catch
            {
                // we suppress any exception if the cancellation was requested.
                if (!cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
            }
            finally
            {
                _objectPool.Return(this);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryExecute(CancellationToken cancellationToken)
        {
            try
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    ResolverContext.Arguments = Selection.Arguments;
                    ResolverContext.PureResolver!(ResolverContext);
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    ResolverContext.ReportError(ex);
                    ResolverContext.Result = null;
                }
            }

            return false;
        }
    }

    internal abstract class ResolverTaskBase : IExecutionTask
    {
        private readonly MiddlewareContext _resolverContext = new();
        private IOperationContext _operationContext = default!;
        private ISelection _selection = default!;

        protected MiddlewareContext ResolverContext => _resolverContext;

        protected IOperationContext OperationContext => _operationContext;

        protected IDiagnosticEvents DiagnosticEvents => OperationContext.DiagnosticEvents;

        protected ISelection Selection => _selection;

        public abstract void BeginExecute(CancellationToken cancellationToken);

        public void Initialize(
            IOperationContext operationContext,
            ISelection selection,
            ResultMap resultMap,
            int responseIndex,
            object? parent,
            Path path,
            IImmutableDictionary<string, object?> scopedContextData)
        {
            _operationContext = operationContext;
            _selection = selection;
            _resolverContext.Initialize(
                operationContext,
                selection,
                resultMap,
                responseIndex,
                parent,
                path,
                scopedContextData);
        }

        public bool Reset()
        {
            _operationContext = default!;
            _selection = default!;
            _resolverContext.Clean();
            return true;
        }

        protected void CompleteValue(bool success, CancellationToken cancellationToken)
        {
            object? completedValue = null;

            try
            {
                // we will only try to complete the resolver value if there are no known errors.
                if (success)
                {
                    if (ValueCompletion.TryComplete(
                        _operationContext,
                        _resolverContext,
                        _resolverContext.Path,
                        _resolverContext.Field.Type,
                        _resolverContext.Result,
                        out completedValue) &&
                        !_resolverContext.Field.Type.IsLeafType() &&
                        completedValue is IHasResultDataParent result)
                    {
                        result.Parent = _resolverContext.ResultMap;
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

                _resolverContext.ReportError(ex);
                _resolverContext.Result = null;
            }

            if (completedValue is null && _resolverContext.Field.Type.IsNonNullType())
            {
                // if we detect a non-null violation we will stash it for later.
                // the non-null propagation is delayed so that we can parallelize better.
                _operationContext.Result.AddNonNullViolation(
                    _resolverContext.Selection.SyntaxNode,
                    _resolverContext.Path,
                    _resolverContext.ResultMap);
            }
            else
            {
                _resolverContext.ResultMap.SetValue(
                    _resolverContext.ResponseIndex,
                    _resolverContext.ResponseName,
                    completedValue,
                    _resolverContext.Field.Type.IsNullableType());
            }
        }
    }
}
