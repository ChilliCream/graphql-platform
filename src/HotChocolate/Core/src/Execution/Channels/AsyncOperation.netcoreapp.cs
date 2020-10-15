#if NETCOREAPP3_1 || NET5_0

using System;
using System.Threading;

namespace HotChocolate.Execution.Channels
{
    internal partial class AsyncOperation<TResult> : IThreadPoolWorkItem
    {
        void IThreadPoolWorkItem.Execute() => SetCompletionAndInvokeContinuation();

        private void UnsafeQueueSetCompletionAndInvokeContinuation() =>
            ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false);

        private static void QueueUserWorkItem(Action<object?> action, object? state) =>
            ThreadPool.QueueUserWorkItem(action, state, preferLocal: false);

        private static CancellationTokenRegistration UnsafeRegister(
            CancellationToken cancellationToken, Action<object?> action, object? state) =>
            cancellationToken.UnsafeRegister(action, state);
    }
}
#endif