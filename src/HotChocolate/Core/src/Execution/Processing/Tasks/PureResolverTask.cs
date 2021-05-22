using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing.Tasks
{
    internal sealed class PureResolverTask : ResolverTaskBase
    {
        private readonly ObjectPool<PureResolverTask> _objectPool;

        public PureResolverTask(ObjectPool<PureResolverTask> objectPool)
        {
            _objectPool = objectPool ?? throw new ArgumentNullException(nameof(objectPool));
        }

        public override ExecutionTaskKind Kind => ExecutionTaskKind.Pure;

        public override void BeginExecute(CancellationToken cancellationToken)
        {
            Execute(cancellationToken);
        }

        public override Task WaitForCompletionAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;

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
                IsCompleted = true;
                OperationContext.Execution.Work.Complete(this);
                _objectPool.Return(this);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryExecute(CancellationToken cancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                if (Selection.Arguments.IsFinalNoErrors)
                {
                    ResolverContext.Arguments = Selection.Arguments;
                    ResolverContext.PureResolver!(ResolverContext);
                    return true;
                }

                if (Selection.Arguments.TryCoerceArguments(
                    ResolverContext,
                    out IReadOnlyDictionary<NameString, ArgumentValue>? coercedArgs))
                {
                    ResolverContext.Arguments = coercedArgs;
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
}
