using System;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    internal static class DisposableExtensions
    {
        public static IAsyncDisposable Combine(
            this IAsyncDisposable? disposable,
            IAsyncDisposable other)
        {
            return disposable is null
                ? other
                : new AsyncDisposable1(disposable, other);
        }

        public static IAsyncDisposable Combine(
            this IAsyncDisposable? disposable,
            IDisposable other)
        {
            return disposable is null
                ? new AsyncDisposable3(other)
                : new AsyncDisposable2(disposable, other);
        }

        public static IDisposable Combine(
            this IDisposable? disposable,
            IDisposable other)
        {
            return disposable is null
                ? other
                : new Disposable1(disposable, other);
        }

        private sealed class AsyncDisposable1 : IAsyncDisposable
        {
            private readonly IAsyncDisposable _a;
            private readonly IAsyncDisposable _b;

            public AsyncDisposable1(IAsyncDisposable a, IAsyncDisposable b)
            {
                _a = a;
                _b = b;
            }

            public async ValueTask DisposeAsync()
            {
                await _a.DisposeAsync();
                await _b.DisposeAsync();
            }
        }

        private sealed class AsyncDisposable2 : IAsyncDisposable
        {
            private readonly IAsyncDisposable _a;
            private readonly IDisposable _b;

            public AsyncDisposable2(IAsyncDisposable a, IDisposable b)
            {
                _a = a;
                _b = b;
            }

            public ValueTask DisposeAsync()
            {
                _b.Dispose();
                return _a.DisposeAsync();
            }
        }

        private sealed class AsyncDisposable3 : IAsyncDisposable
        {
            private readonly IDisposable _a;

            public AsyncDisposable3(IDisposable a)
            {
                _a = a;
            }

            public ValueTask DisposeAsync()
            {
                _a.Dispose();
                return default;
            }
        }

        private sealed class Disposable1 : IDisposable
        {
            private readonly IDisposable _a;
            private readonly IDisposable _b;

            public Disposable1(IDisposable a, IDisposable b)
            {
                _a = a;
                _b = b;
            }

            public void Dispose()
            {
                _a.Dispose();
                _b.Dispose();
            }
        }
    }
}
