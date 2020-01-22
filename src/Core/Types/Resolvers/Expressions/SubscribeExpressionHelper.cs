using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Utilities.Subscriptions;

namespace HotChocolate.Resolvers.Expressions
{
    internal static class SubscribeExpressionHelper
    {
        public static async ValueTask<IAsyncEnumerable<object>> AwaitTaskAsyncEnumerable<T>(
            ValueTask<IAsyncEnumerable<T>> task)
        {
            if (task == null)
            {
                return null;
            }

            IAsyncEnumerable<T> enumerable = await task.ConfigureAwait(false);
            return ConvertEnumerable(enumerable);
        }

        public static async ValueTask<IAsyncEnumerable<object>> AwaitTaskEnumerable<T>(
            ValueTask<IEnumerable<T>> task)
        {
            if (task == null)
            {
                return null;
            }

            IEnumerable<T> enumerable = await task.ConfigureAwait(false);
            return ConvertEnumerable(enumerable);
        }

        public static async ValueTask<IAsyncEnumerable<object>> AwaitTaskQueryable<T>(
            ValueTask<IQueryable<T>> task)
        {
            if (task == null)
            {
                return null;
            }

            IEnumerable<T> enumerable = await task.ConfigureAwait(false);
            return ConvertEnumerable(enumerable);
        }

        public static async ValueTask<IAsyncEnumerable<object>> AwaitTaskObservable<T>(
            ValueTask<IObservable<T>> task)
        {
            if (task == null)
            {
                return null;
            }

            IObservable<T> enumerable = await task.ConfigureAwait(false);
            return ConvertObservable(enumerable);
        }

        public static async ValueTask<IAsyncEnumerable<object>> AwaitValueTaskAsyncEnumerable<T>(
            ValueTask<IAsyncEnumerable<T>> task)
        {
            if (task == null)
            {
                return null;
            }

            IAsyncEnumerable<T> enumerable = await task.ConfigureAwait(false);
            return ConvertEnumerable(enumerable);
        }

        public static async ValueTask<IAsyncEnumerable<object>> AwaitValueTaskEnumerable<T>(
            ValueTask<IEnumerable<T>> task)
        {
            if (task == null)
            {
                return null;
            }

            IEnumerable<T> enumerable = await task.ConfigureAwait(false);
            return ConvertEnumerable(enumerable);
        }

        public static async ValueTask<IAsyncEnumerable<object>> AwaitValueTaskQueryable<T>(
            ValueTask<IQueryable<T>> task)
        {
            if (task == null)
            {
                return null;
            }

            IEnumerable<T> enumerable = await task.ConfigureAwait(false);
            return ConvertEnumerable(enumerable);
        }

        public static async ValueTask<IAsyncEnumerable<object>> AwaitValueTaskObservable<T>(
            ValueTask<IObservable<T>> task)
        {
            if (task == null)
            {
                return null;
            }

            IObservable<T> enumerable = await task.ConfigureAwait(false);
            return ConvertObservable(enumerable);
        }

        public static ValueTask<IAsyncEnumerable<object>> WrapAsyncEnumerable<T>(
            IAsyncEnumerable<T> result) =>
            new ValueTask<IAsyncEnumerable<object>>(ConvertEnumerable(result));

        public static ValueTask<IAsyncEnumerable<object>> WrapEnumerable<T>(
            IEnumerable<T> result) =>
            new ValueTask<IAsyncEnumerable<object>>(ConvertEnumerable(result));

        public static ValueTask<IAsyncEnumerable<object>> WrapQueryable<T>(
            IQueryable<T> result) =>
            new ValueTask<IAsyncEnumerable<object>>(ConvertEnumerable(result));

        public static ValueTask<IAsyncEnumerable<object>> WrapObservable<T>(
            IObservable<T> result) =>
            new ValueTask<IAsyncEnumerable<object>>(ConvertObservable(result));

        private static IAsyncEnumerable<object> ConvertObservable<T>(
            IObservable<T> enumerable) =>
            new ObservableSourceStreamAdapter<T>(enumerable);

        private static IAsyncEnumerable<object> ConvertEnumerable<T>(
            IEnumerable<T> enumerable) =>
            new EnumerableSourceStreamAdapter<T>(enumerable);

        private static async IAsyncEnumerable<object> ConvertEnumerable<T>(
            IAsyncEnumerable<T> enumerable)
        {
            await foreach (T item in enumerable.ConfigureAwait(false))
            {
                yield return item;
            }
        }
    }
}
