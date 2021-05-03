using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Processing.Internal;

namespace HotChocolate.Execution.Processing
{
    /// <inheritdoc/>
    internal class WorkBacklog : IWorkBacklog
    {
        private readonly object _sync = new();
        private readonly WorkQueue _work = new();
        private int _processors = 1;
        private bool _mainIsWaiting;

        public WorkBacklog()
        {
            _work.BacklogEmpty += (sender, args) => BacklogEmpty?.Invoke(sender, args);
        }

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
        public bool TryTake([NotNullWhen(true)] out IExecutionTask? task) =>
            _work.TryTake(out task);

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

            var backlogSize = _work.Push(task);

            if (backlogSize > _processors * 2 || _mainIsWaiting && _processors == 1)
            {
                lock (_sync)
                {
                    if (backlogSize > _processors * 2 || _mainIsWaiting && _processors == 1)
                    {
                        TryScaleUnsafe();
                    }
                }
            }
        }

        /// <inheritdoc/>
        public bool TryCompleteProcessor()
        {
            lock (_sync)
            {
                if (_mainIsWaiting && _processors == 1 && !_work.IsEmpty)
                {
                    return false;
                }

                _processors--;
                return true;
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
            _work.ClearUnsafe();
            _processors = 1;
            BackPressureLimitExceeded = null;
        }

        private void TryScaleUnsafe()
        {
            if (_processors < 4)
            {
                _processors++;
                BackPressureLimitExceeded?.Invoke(null, EventArgs.Empty);
            }

        }
    }
}
