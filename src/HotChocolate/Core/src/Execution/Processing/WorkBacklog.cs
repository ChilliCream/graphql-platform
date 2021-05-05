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
        private readonly WorkQueueUnsafe _work = new();

        private int _processors = 1;
        private bool _mainIsWaiting;

        public WorkBacklog()
        {
            _work.BacklogEmpty += (sender, args) => BacklogEmpty?.Invoke(sender, args);
        }

        /// <summary>
        /// Gets or sets the current request context.
        /// </summary>
        internal IRequestContext RequestContext { get; set; } = default!;

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
        public bool TryTakeSerial([NotNullWhen(true)] out IExecutionTask? task)
        {
            task = null;
            return false;
        }

        /// <inheritdoc/>
        public async Task WaitForWorkAsync(CancellationToken cancellationToken)
        {
            _mainIsWaiting = true;

            try
            {
                if (!_work.IsEmpty)
                {
                    return;
                }

                if (_work.TryPeekInProgress(out IExecutionTask? executionTask))
                {
                    await executionTask
                        .WaitForCompletionAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    await Task.Yield();
                }
            }
            finally
            {
                _mainIsWaiting = false;
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
            var changedScale = false;
            var changeScale = false;

            try
            {
                _lock.Enter(ref lockTaken);

                var backlogSize = _work.Push(task);
                var processors = _processors;

                if (backlogSize > CalculateScalePressure() || _mainIsWaiting && _processors == 1)
                {
                    changeScale = true;
                }
            }
            finally
            {
                if (lockTaken) _lock.Exit(false);
            }

            if(changeScale) {
                lock (_scale)
                {
                    if (backlogSize > CalculateScalePressure() ||
                        _mainIsWaiting && _processors == 1)
                    {
                        changedScale = TryScaleUnsafe();
                        processors = _processors;
                    }
                }
            }

            if (changedScale)
            {
                // we invoke the scale diagnostic event after leaving the lock to not block
                // if a an event listener is badly implemented.
                DiagnosticEvents.ScaleTaskProcessors(RequestContext, backlogSize, processors);
            }
        }

        /// <inheritdoc/>
        public bool TryCompleteProcessor()
        {
            var changedScale = false;
            var processors = _processors;
            var backlogSize = 0;

            try
            {
                lock (_scale)
                {
                    if (_mainIsWaiting && _processors == 1 && !_work.IsEmpty)
                    {
                        return false;
                    }

                    changedScale = true;
                    processors = --_processors;
                    backlogSize = _work.Count;
                    return true;
                }
            }
            finally
            {
                if (changedScale)
                {
                    // we invoke the scale diagnostic event after leaving the lock to not block
                    // if a an event listener is badly implemented.
                    DiagnosticEvents.ScaleTaskProcessors(RequestContext, backlogSize, processors);
                }
            }
        }

        /// <inheritdoc/>
        public void Complete(IExecutionTask task)
        {
            if (task is null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            if (task.Parent is null)
            {
                _work.Complete(task);
            }
        }

        public void Clear()
        {
            _work.Clear();
            _processors = 1;
            RequestContext = default!;
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

        private class NoOp : IExecutionStep
        {
            public bool IsAllowed(IExecutionTask task) => true;
        }
    }
}
