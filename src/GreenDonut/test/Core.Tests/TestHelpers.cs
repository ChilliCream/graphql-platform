using System;
using System.Threading.Tasks;

namespace GreenDonut
{
    internal static class TestHelpers
    {
        public static FetchDataDelegate<TKey, TValue> CreateFetch<TKey, TValue>()
        {
            return async (keys, cancellationToken) => await Task.FromResult(new Result<TValue>[0]);
        }

        public static FetchDataDelegate<TKey, TValue> CreateFetch<TKey, TValue>(Exception error)
        {
            return async (keys, cancellationToken) =>
                await Task.FromResult(new[] { (Result<TValue>)error });
        }

        public static FetchDataDelegate<TKey, TValue> CreateFetch<TKey, TValue>(
            Result<TValue> value)
        {
            return async (keys, cancellationToken) => await Task.FromResult(new[] { value });
        }

        public static FetchDataDelegate<TKey, TValue> CreateFetch<TKey, TValue>(
            Result<TValue>[] values)
        {
            return async (keys, cancellationToken) => await Task.FromResult(values);
        }
    }
}
