using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    public static class WaitHandleExtensions
    {
        private static Task<bool> Canceled = Task.FromCanceled<bool>(new CancellationToken(true));
        private static Task<bool> False = Task.FromResult(false);
        private static Task<bool> True = Task.FromResult(true);

        public static Task FromWaitHandle(this WaitHandle handle)
        {
            return handle.FromWaitHandle(Timeout.InfiniteTimeSpan, CancellationToken.None);
        }

        public static Task<bool> FromWaitHandle(this WaitHandle handle, TimeSpan timeout)
        {
            return handle.FromWaitHandle(timeout, CancellationToken.None);
        }

        public static Task FromWaitHandle(this WaitHandle handle, CancellationToken token)
        {
            return handle.FromWaitHandle(Timeout.InfiniteTimeSpan, token);
        }

        public static Task<bool> FromWaitHandle(
            this WaitHandle handle,
            TimeSpan timeout,
            CancellationToken token)
        {
            var alreadySignalled = handle.WaitOne(0);

            if (alreadySignalled)
            {
                return True;
            }

            if (timeout == TimeSpan.Zero)
            {
                return False;
            }

            if (token.IsCancellationRequested)
            {
                return Canceled;
            }

            return handle.DoFromWaitHandle(timeout, token);
        }

        private static async Task<bool> DoFromWaitHandle(
            this WaitHandle handle,
            TimeSpan timeout,
            CancellationToken token)
        {
            var completionSource = new TaskCompletionSource<bool>();

            using (new ThreadPoolRegistration(handle, timeout, completionSource))
            using (token.Register(
                state => ((TaskCompletionSource<bool>)state).TrySetCanceled(),
                completionSource,
                false))
            {
                return await completionSource.Task.ConfigureAwait(false);
            }
        }

        private sealed class ThreadPoolRegistration
            : IDisposable
        {
            private readonly RegisteredWaitHandle _registeredWaitHandle;

            public ThreadPoolRegistration(
                WaitHandle handle,
                TimeSpan timeout,
                TaskCompletionSource<bool> tcs)
            {
                _registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(
                    handle,
                    (state, timedOut) => ((TaskCompletionSource<bool>)state).TrySetResult(!timedOut),
                    tcs,
                    timeout,
                    true);
            }

            void IDisposable.Dispose() => _registeredWaitHandle.Unregister(null);
        }
    }
}
