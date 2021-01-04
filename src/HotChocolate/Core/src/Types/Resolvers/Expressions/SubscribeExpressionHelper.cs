using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Utilities.Subscriptions;

namespace HotChocolate.Resolvers.Expressions
{
    internal static class SubscribeExpressionHelper
    {
        public static async ValueTask<ISourceStream> AwaitTaskSourceStreamGeneric<T>(
            Task<ISourceStream<T>> task)
        {
            if (task is null)
            {
                return null;
            }

            return await task.ConfigureAwait(false);
        }

        public static async ValueTask<ISourceStream> AwaitTaskSourceStream(
            Task<ISourceStream> task)
        {
            if (task is null)
            {
                return null;
            }

            return await task.ConfigureAwait(false);
        }

        public static async ValueTask<ISourceStream> AwaitTaskAsyncEnumerable<T>(
            Task<IAsyncEnumerable<T>> task)
        {
            if (task is null)
            {
                return null;
            }

            IAsyncEnumerable<T> enumerable = await task.ConfigureAwait(false);
            return ConvertEnumerable(enumerable);
        }

        public static async ValueTask<ISourceStream> AwaitTaskEnumerable<T>(
            Task<IEnumerable<T>> task)
        {
            if (task is null)
            {
                return null;
            }

            IEnumerable<T> enumerable = await task.ConfigureAwait(false);
            return ConvertEnumerable(enumerable);
        }

        public static async ValueTask<ISourceStream> AwaitTaskQueryable<T>(
            Task<IQueryable<T>> task)
        {
            if (task is null)
            {
                return null;
            }

            IEnumerable<T> enumerable = await task.ConfigureAwait(false);
            return ConvertEnumerable(enumerable);
        }

        public static async ValueTask<ISourceStream> AwaitTaskObservable<T>(
            Task<IObservable<T>> task)
        {
            if (task is null)
            {
                return null;
            }

            IObservable<T> enumerable = await task.ConfigureAwait(false);
            return ConvertObservable(enumerable);
        }

        public static async ValueTask<ISourceStream> AwaitValueTaskSourceStreamGeneric<T>(
            ValueTask<ISourceStream<T>> task)
        {
            return await task.ConfigureAwait(false);
        }

        public static async ValueTask<ISourceStream> AwaitValueTaskAsyncEnumerable<T>(
            ValueTask<IAsyncEnumerable<T>> task)
        {
            IAsyncEnumerable<T> enumerable = await task.ConfigureAwait(false);
            return ConvertEnumerable(enumerable);
        }

        public static async ValueTask<ISourceStream> AwaitValueTaskEnumerable<T>(
            ValueTask<IEnumerable<T>> task)
        {
            IEnumerable<T> enumerable = await task.ConfigureAwait(false);
            return ConvertEnumerable(enumerable);
        }

        public static async ValueTask<ISourceStream> AwaitValueTaskQueryable<T>(
            ValueTask<IQueryable<T>> task)
        {
            IEnumerable<T> enumerable = await task.ConfigureAwait(false);
            return ConvertEnumerable(enumerable);
        }

        public static async ValueTask<ISourceStream> AwaitValueTaskObservable<T>(
            ValueTask<IObservable<T>> task)
        {
            IObservable<T> enumerable = await task.ConfigureAwait(false);
            return ConvertObservable(enumerable);
        }

        public static ValueTask<ISourceStream> WrapSourceStreamGeneric<T>(
            ISourceStream<T> result) =>
            new ValueTask<ISourceStream>(result);

        public static ValueTask<ISourceStream> WrapSourceStream(
            ISourceStream result) =>
            new ValueTask<ISourceStream>(result);

        public static ValueTask<ISourceStream> WrapAsyncEnumerable<T>(
            IAsyncEnumerable<T> result) =>
            new ValueTask<ISourceStream>(ConvertEnumerable(result));

        public static ValueTask<ISourceStream> WrapEnumerable<T>(
            IEnumerable<T> result) =>
            new ValueTask<ISourceStream>(ConvertEnumerable(result));

        public static ValueTask<ISourceStream> WrapQueryable<T>(
            IQueryable<T> result) =>
            new ValueTask<ISourceStream>(ConvertEnumerable(result));

        public static ValueTask<ISourceStream> WrapObservable<T>(
            IObservable<T> result) =>
            new ValueTask<ISourceStream>(ConvertObservable(result));

        private static ISourceStream ConvertObservable<T>(
            IObservable<T> enumerable) =>
            new SourceStreamWrapper(new ObservableSourceStreamAdapter<T>(enumerable));

        private static ISourceStream ConvertEnumerable<T>(
            IEnumerable<T> enumerable) =>
            new SourceStreamWrapper(new EnumerableSourceStreamAdapter<T>(enumerable));

        private static ISourceStream ConvertEnumerable<T>(
            IAsyncEnumerable<T> enumerable)
        {
            return new SourceStreamWrapper(new Enumerate<T>(enumerable));
        }

        private class Enumerate<T> : IAsyncEnumerable<object>
        {
            private readonly IAsyncEnumerable<T> _enumerable;

            public Enumerate(IAsyncEnumerable<T> enumerable)
            {
                _enumerable = enumerable;
            }

            public async IAsyncEnumerator<object> GetAsyncEnumerator(
                CancellationToken cancellationToken = default)
            {
                await foreach (T item in _enumerable.WithCancellation(cancellationToken)
                    .ConfigureAwait(false))
                {
                    yield return item;
                }
            }
        }
    }
}
