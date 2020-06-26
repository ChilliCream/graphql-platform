#if NETSTANDARD2_0 || NETSTANDARD2_1
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Channels
{
    internal partial class AsyncOperation<TResult>
    {
        private void UnsafeQueueSetCompletionAndInvokeContinuation() =>
            Task.Factory.StartNew(s => ((AsyncOperation<TResult>)s).SetCompletionAndInvokeContinuation(), this,
                CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

        private static void QueueUserWorkItem(Action<object?> action, object? state) =>
            Task.Factory.StartNew(action, state,
                CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

        private static CancellationTokenRegistration UnsafeRegister(
            CancellationToken cancellationToken, Action<object?> action, object? state) =>
            cancellationToken.Register(action, state);
    }
}
#endif