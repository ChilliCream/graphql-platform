using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Utilities.Subscriptions;

namespace HotChocolate.Resolvers.Expressions
{
    internal static class SubscribeExpressionHelper
    {
        public static async Task<IAsyncEnumerable<object>> AwaitAsyncEnumerable<T>(
            Task<IAsyncEnumerable<T>> task)
        {
            if (task == null)
            {
                return null;
            }

            IAsyncEnumerable<T> enumerable = await task.ConfigureAwait(false);
            return ConvertEnumerable(enumerable);
        }

        public static async Task<IAsyncEnumerable<object>> AwaitEnumerable<T>(
            Task<IEnumerable<T>> task)
        {
            if (task == null)
            {
                return null;
            }

            IEnumerable<T> enumerable = await task.ConfigureAwait(false);
            return ConvertEnumerable(enumerable);
        }

        public static async Task<IAsyncEnumerable<object>> AwaitQueryable<T>(
            Task<IQueryable<T>> task)
        {
            if (task == null)
            {
                return null;
            }

            IEnumerable<T> enumerable = await task.ConfigureAwait(false);
            return ConvertEnumerable(enumerable);
        }

        public static async Task<IAsyncEnumerable<object>> AwaitObservable<T>(
            Task<IObservable<T>> task)
        {
            if (task == null)
            {
                return null;
            }

            IObservable<T> enumerable = await task.ConfigureAwait(false);
            return ConvertObservable(enumerable);
        }

        public static Task<IAsyncEnumerable<object>> WrapAsyncEnumerable<T>(
            IAsyncEnumerable<T> result) =>
            Task.FromResult<IAsyncEnumerable<object>>(ConvertEnumerable(result));

        public static Task<IAsyncEnumerable<object>> WrapEnumerable<T>(
            IEnumerable<T> result) =>
            Task.FromResult<IAsyncEnumerable<object>>(ConvertEnumerable(result));

        public static Task<IAsyncEnumerable<object>> WrapQueryable<T>(
            IQueryable<T> result) =>
            Task.FromResult<IAsyncEnumerable<object>>(ConvertEnumerable(result));

        public static Task<IAsyncEnumerable<object>> WrapObservable<T>(
            IObservable<T> result) =>
            Task.FromResult<IAsyncEnumerable<object>>(ConvertObservable(result));

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
