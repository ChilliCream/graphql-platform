using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Types;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing.Tasks
{
    internal sealed partial class ResolverTask : IExecutionTask
    {
        private readonly ObjectPool<ResolverTask> _objectPool;
        private readonly MiddlewareContext _resolverContext = new();
        private readonly List<ResolverTask> _taskBuffer = new();
        private IOperationContext _operationContext = default!;
        private ISelection _selection = default!;
        private ExecutionTaskStatus _completionStatus = ExecutionTaskStatus.Completed;
        private Task? _task;

        public ResolverTask(ObjectPool<ResolverTask> objectPool)
        {
            _objectPool = objectPool ?? throw new ArgumentNullException(nameof(objectPool));
        }

        /// <summary>
        /// Gets access to the resolver context for this task.
        /// </summary>
        internal MiddlewareContext ResolverContext => _resolverContext;

        /// <summary>
        /// Gets access to the operation context.
        /// </summary>
        private IOperationContext OperationContext => _operationContext;

        /// <summary>
        /// Gets access to the diagnostic events.
        /// </summary>
        private IExecutionDiagnosticEvents DiagnosticEvents => OperationContext.DiagnosticEvents;

        /// <summary>
        /// Gets the selection for which a resolver is executed.
        /// </summary>
        internal ISelection Selection => _selection;

        /// <inheritdoc />
        public ExecutionTaskKind Kind => ExecutionTaskKind.Parallel;

        /// <inheritdoc />
        public ExecutionTaskStatus Status { get; private set; }

        /// <inheritdoc />
        public IExecutionTask? Next { get; set; }

        /// <inheritdoc />
        public IExecutionTask? Previous { get; set; }

        /// <summary>
        /// Gets access to the internal result map into which the task will write the result.
        /// </summary>
        public ResultMap ResultMap { get; private set; } = default!;

        /// <summary>
        /// Gets the completed value of this task.
        /// </summary>
        public object? CompletedValue { get; private set; }

        /// <inheritdoc />
        public object? State { get; set; }

        /// <inheritdoc />
        public bool IsSerial { get; set; }

        /// <inheritdoc />
        public bool IsRegistered { get; set; }

        /// <summary>
        /// Tasks that were created through the field completion.
        /// </summary>
        public List<ResolverTask> ChildTasks => _taskBuffer;

        /// <inheritdoc />
        public void BeginExecute(CancellationToken cancellationToken)
        {
            Status = ExecutionTaskStatus.Running;
            _task = ExecuteAsync(cancellationToken);
        }

        /// <inheritdoc />
        public Task WaitForCompletionAsync(CancellationToken cancellationToken) =>
            _task ?? Task.CompletedTask;

        /// <summary>
        /// Completes the resolver result.
        /// </summary>
        /// <param name="success">Defines if the resolver succeeded without errors.</param>
        /// <param name="cancellationToken">The execution cancellation token.</param>
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
                }
            }
            catch (OperationCanceledException)
            {
                // If we run into this exception the request was aborted.
                // In this case we do nothing and just return.
                _completionStatus = ExecutionTaskStatus.Faulted;
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
                completedValue = null;
            }

            CompletedValue = completedValue;

            if (completedValue is null && _selection.Type.IsNonNullType())
            {
                // if we detect a non-null violation we will stash it for later.
                // the non-null propagation is delayed so that we can parallelize better.
                _completionStatus = ExecutionTaskStatus.Faulted;
                _operationContext.Result.AddNonNullViolation(
                    _resolverContext.Selection.SyntaxNode,
                    _resolverContext.Path,
                    _resolverContext.ResultMap);
                _taskBuffer.Clear();
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
            _completionStatus = ExecutionTaskStatus.Completed;
            _operationContext = default!;
            _selection = default!;
            _resolverContext.Clean();
            ResultMap = default!;
            CompletedValue = null;
            Status = ExecutionTaskStatus.WaitingToRun;
            IsSerial = false;
            IsRegistered = false;
            Next = null;
            Previous = null;
            State = null;
            _taskBuffer.Clear();
            return true;
        }
    }
}
