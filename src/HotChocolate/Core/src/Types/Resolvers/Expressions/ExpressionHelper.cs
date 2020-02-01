using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotChocolate.Resolvers.Expressions
{
    internal static class ExpressionHelper
    {
        public static async Task<object> AwaitHelper<T>(Task<T> task)
        {
            if (task == null)
            {
                return null;
            }
            return await task.ConfigureAwait(false);
        }

        public static Task<object> WrapResultHelper<T>(T result)
        {
            return Task.FromResult<object>(result);
        }

        public static TContextData ResolveContextData<TContextData>(
            IDictionary<string, object> contextData,
            string key,
            bool defaultIfNotExists)
        {
            if (contextData.TryGetValue(key, out object value))
            {
                if (value is null)
                {
                    return default;
                }

                if (value is TContextData v)
                {
                    return v;
                }
            }
            else if (defaultIfNotExists)
            {
                return default;
            }

            // TODO : resources
            throw new ArgumentException(
                "The specified context key does not exist.");
        }

        public static TContextData ResolveScopedContextData<TContextData>(
            IReadOnlyDictionary<string, object> contextData,
            string key,
            bool defaultIfNotExists)
        {
            if (contextData.TryGetValue(key, out object value))
            {
                if (value is null)
                {
                    return default;
                }

                if (value is TContextData v)
                {
                    return v;
                }
            }
            else if (defaultIfNotExists)
            {
                return default;
            }

            // TODO : resources
            throw new ArgumentException(
                "The specified context key does not exist.");
        }
    }
}
