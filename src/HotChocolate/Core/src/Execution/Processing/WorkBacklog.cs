using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing.Internal;

namespace HotChocolate.Execution.Processing
{
    /// <inheritdoc/>
    internal class WorkBacklog : IWorkBacklog
    {
        private SpinLock _lock = new(Debugger.IsAttached);
        private readonly UnsafeWorkQueue _work = new();
        private readonly PausedWorkQueue _paused = new();
        private readonly Func<IRequestContext> _requestContext;
        private QueryPlanStep _step = default!;

        private int _processors = 1;
        private bool _mainIsPaused;

        public WorkBacklog(Func<IRequestContext> requestContext)
        {
            _requestContext = requestContext ??
                throw new ArgumentNullException(nameof(requestContext));

            _work.BacklogEmpty += (sender, args) => BacklogEmpty?.Invoke(sender, args);
        }

        /// <summary>
        /// Gets or sets the current request context.
        /// </summary>
        private IRequestContext RequestContext => _requestContext();

        /// <summary>
        /// The diagnostic events.
        /// </summary>
        private IDiagnosticEvents DiagnosticEvents => RequestContext.DiagnosticEvents;

        /// <inheritdoc/>
        public event EventHandler<EventArgs>? BackPressureLimitExceeded;

        /// <inheritdoc/>
        public event EventHandler<EventArgs>? BacklogEmpty;

        /// <inheritdoc/>
        public bool IsEmpty => _work.IsEmpty;

        /// <inheritdoc/>
        public bool IsRunning
        {
            get
            {
                return _work.IsRunning || _processors > 1;
            }
        }

        /// <inheritdoc/>
        public bool TryTake([NotNullWhen(true)] out IExecutionTask? task)
        {
            var lockTaken = false;

            try
            {
                _lock.Enter(ref lockTaken);
                return _work.TryTake(out task);
            }
            finally
            {
                if (lockTaken) _lock.Exit(false);
            }
        }

        /// <inheritdoc/>
        public void Register(IExecutionTask task)
        {
            if (task is null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            var lockTaken = false;
            var scaled = false;
            var backlogSize = 0;
            var processors = _processors;

            try
            {
                _lock.Enter(ref lockTaken);

                // if (_step.IsAllowed(task))
                if(true)
                {
                    backlogSize = _work.Push(task);

                    if (backlogSize > CalculateScalePressure() || _mainIsPaused && _processors == 1)
                    {
                        scaled = TryScaleUnsafe();
                        processors = _processors;
                    }
                }
                else
                {
                    _paused.Enqueue(task);
                }
            }
            finally
            {
                if (lockTaken) _lock.Exit(false);
            }

            if (scaled)
            {
                // we invoke the scale diagnostic event after leaving the lock to not block
                // if a an event listener is badly implemented.
                DiagnosticEvents.ScaleTaskProcessors(RequestContext, backlogSize, processors);
            }
        }

        /// <inheritdoc/>
        public void Complete(IExecutionTask task)
        {
            if (task is null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            if (task.Parent is not null)
            {
                return;
            }

            var lockTaken = false;

            try
            {
                _lock.Enter(ref lockTaken);
                _work.Complete(task);
            }
            finally
            {
                if (lockTaken) _lock.Exit(false);
            }
        }

        /// <inheritdoc/>
        public async Task WaitForWorkAsync(CancellationToken cancellationToken)
        {
            var lockTaken = false;

            try
            {
                _lock.Enter(ref lockTaken);

                // if we have work we will not wait and end waiting for new work.
                if (!_work.IsEmpty || _work.IsEmpty && !IsRunning)
                {
                    return;
                }

                // mark the main processor as being paused.
                _mainIsPaused = true;
            }
            finally
            {
                if (lockTaken) _lock.Exit(false);
            }

            try
            {
                // lets take the first thing in the in progress list an wait for it.
                if (_work.TryPeekInProgress(out IExecutionTask? executionTask))
                {
                    await executionTask
                        .WaitForCompletionAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    // if we have no work in progress we will just wait for 2 spins for a second.
                    Thread.SpinWait(2);

                    // now lets see if there is new work or if we have completed.
                    if (!_work.IsEmpty || _work.IsEmpty && !IsRunning)
                    {
                        return;
                    }

                    // if we are still running and there is no work in progress that we can await
                    // we will yield the task back to the task scheduler for a second and then
                    // complete waiting.
                    await Task.Yield();
                }
            }
            finally
            {
                // mark the main task as in progress again.
                _mainIsPaused = false;
            }
        }

        /// <inheritdoc/>
        public bool TryCompleteProcessor()
        {
            var changedScale = false;
            var processors = _processors;
            var backlogSize = 0;

            var lockTaken = false;

            try
            {
                _lock.Enter(ref lockTaken);

                if (_mainIsPaused && _processors == 2 && !_work.IsEmpty)
                {
                    return false;
                }

                changedScale = true;
                processors = --_processors;
                backlogSize = _work.Count;
                return true;
            }
            finally
            {
                if (lockTaken) _lock.Exit(false);

                if (changedScale)
                {
                    // we invoke the scale diagnostic event after leaving the lock to not block
                    // if a an event listener is badly implemented.
                    DiagnosticEvents.ScaleTaskProcessors(RequestContext, backlogSize, processors);
                }
            }
        }

        public void Clear()
        {
            _work.Clear();
            _processors = 1;
            BackPressureLimitExceeded = null;
        }

        private bool TryScaleUnsafe()
        {
            if (_processors < 4)
            {
                _processors++;
                BackPressureLimitExceeded?.Invoke(null, EventArgs.Empty);
                return true;
            }

            return false;
        }

        private int CalculateScalePressure() =>
            _processors switch
            {
                1 => 4,
                2 => 8,
                3 => 16,
                4 => 32,
                5 => 64,
                6 => 128,
                7 => 256,
                _ => 512
            };
    }
}
