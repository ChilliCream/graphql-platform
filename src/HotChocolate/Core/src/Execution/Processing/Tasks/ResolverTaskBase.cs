using System;
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
        private IOperationContext _operationContext = default!;
        private ISelection _selection = default!;

        protected MiddlewareContext ResolverContext => _resolverContext;

        protected IOperationContext OperationContext => _operationContext;

        protected IDiagnosticEvents DiagnosticEvents => OperationContext.DiagnosticEvents;

        public ISelection Selection => _selection;

        public abstract ExecutionTaskKind Kind { get; }

        public bool IsCompleted { get; protected set; }

        public IExecutionTask? Parent { get; set; }

        public IExecutionTask? Next { get; set; }

        public IExecutionTask? Previous { get; set; }

        public object? State { get; set; }

        public bool IsSerial { get; set; }

        public abstract void BeginExecute(CancellationToken cancellationToken);

        public abstract Task WaitForCompletionAsync(CancellationToken cancellationToken);

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
            IsCompleted = false;
            Parent = null;
            Next = null;
            Previous = null;
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
                    IType fieldType = _resolverContext.Field.Type;

                    if (ValueCompletion.TryComplete(
                        _operationContext,
                        _resolverContext,
                        (ISelection)_resolverContext.Selection,
                        _resolverContext.Path,
                        fieldType,
                        _resolverContext.ResponseName,
                        _resolverContext.ResponseIndex,
                        _resolverContext.Result,
                        out completedValue) &&
                        fieldType.Kind is not TypeKind.Scalar and not TypeKind.Enum &&
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
