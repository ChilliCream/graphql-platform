using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing.Tasks
{
    internal abstract class ResolverTaskBase : IExecutionTask
    {
        private readonly MiddlewareContext _resolverContext = new();
        private readonly List<IExecutionTask> _taskBuffer = new();
        private IOperationContext _operationContext = default!;
        private ISelection _selection = default!;

        /// <summary>
        /// Gets access to the resolver context for this task.
        /// </summary>
        protected internal MiddlewareContext ResolverContext => _resolverContext;

        /// <summary>
        /// Gets access to the operation context.
        /// </summary>
        protected IOperationContext OperationContext => _operationContext;

        /// <summary>
        /// Gets access to the diagnostic events.
        /// </summary>
        protected IExecutionDiagnosticEvents DiagnosticEvents => OperationContext.DiagnosticEvents;

        /// <summary>
        /// Gets the selection for which a resolver is executed.
        /// </summary>
        public ISelection Selection => _selection;

        /// <inheritdoc />
        public abstract ExecutionTaskKind Kind { get; }

        /// <inheritdoc />
        public bool IsCompleted { get; protected set; }

        /// <inheritdoc />
        public IExecutionTask? Next { get; set; }

        /// <inheritdoc />
        public IExecutionTask? Previous { get; set; }

        /// <summary>
        /// Gets access to the internal result map into which the task will write the result.
        /// </summary>
        public ResultMap ResultMap { get; private set; } = default!;

        /// <inheritdoc />
        public object? State { get; set; }

        /// <inheritdoc />
        public bool IsSerial { get; set; }

        /// <inheritdoc />
        public bool IsRegistered { get; set; }

        /// <inheritdoc />
        public abstract void BeginExecute(CancellationToken cancellationToken);

        /// <inheritdoc />
        public abstract Task WaitForCompletionAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Initializes this task after it is retrieved from its pool.
        /// </summary>
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
            ResultMap = resultMap;
        }

        /// <summary>
        /// Resets the resolver task before returning it to the pool.
        /// </summary>
        /// <returns></returns>
        public bool Reset()
        {
            _operationContext = default!;
            _selection = default!;
            _resolverContext.Clean();
            ResultMap = default!;
            IsCompleted = false;
            IsSerial = false;
            IsRegistered = false;
            Next = null;
            Previous = null;
            State = null;
            _taskBuffer.Clear();
            return true;
        }

        /// <summary>
        /// Completes the resolver result.
        /// </summary>
        /// <param name="success">Defines if the resolver succeeded without errors.</param>
        /// <param name="cancellationToken">The execution cancellation token.</param>
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
                        (ISelection)_resolverContext.Selection,
                        _resolverContext.Path,
                        _selection.Type,
                        _resolverContext.ResponseName,
                        _resolverContext.ResponseIndex,
                        _resolverContext.Result,
                        _taskBuffer,
                        out completedValue) &&
                        _selection.TypeKind is not TypeKind.Scalar and not TypeKind.Enum &&
                        completedValue is IHasResultDataParent result)
                    {
                        result.Parent = _resolverContext.ResultMap;
                    }

                    if (_taskBuffer.Count > 0)
                    {
                        _operationContext.Scheduler.Register(_taskBuffer);
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
