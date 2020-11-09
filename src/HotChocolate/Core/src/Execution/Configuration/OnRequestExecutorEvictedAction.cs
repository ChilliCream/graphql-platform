using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Configuration
{
    public readonly struct OnRequestExecutorEvictedAction
    {
        public OnRequestExecutorEvictedAction(
            Action<IRequestExecutor> action)
        {
            Action = action ?? throw new ArgumentNullException(nameof(action));
            AsyncAction = default;
        }

        public OnRequestExecutorEvictedAction(
            Func<IRequestExecutor, CancellationToken, ValueTask> asyncAction)
        {
            Action = default;
            AsyncAction = asyncAction ?? throw new ArgumentNullException(nameof(asyncAction));
        }

        public Action<IRequestExecutor>? Action { get; }

        public Func<IRequestExecutor, CancellationToken, ValueTask>? AsyncAction { get; }
    }
}
