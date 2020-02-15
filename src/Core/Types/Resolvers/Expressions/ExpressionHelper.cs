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

        [Obsolete]
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

        public static TContextData GetGlobalState<TContextData>(
            IDictionary<string, object> contextData,
            string key) =>
            GetGlobalStateWithDefault<TContextData>(contextData, key, false, default);

        public static TContextData GetGlobalStateWithDefault<TContextData>(
            IDictionary<string, object> contextData,
            string key,
            bool hasDefaultValue,
            TContextData defaultValue)
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
            else if (hasDefaultValue)
            {
                return defaultValue;
            }

            // TODO : resources
            throw new ArgumentException(
                $"The specified key `{key}` does not exist on `context.ContextData`.");
        }

        public static SetState<TContextData> SetGlobalStateGeneric<TContextData>(
            IDictionary<string, object> contextData,
            string key)
        {
            return new SetState<TContextData>(value =>
            {
                contextData[key] = value;
            });
        }

        public static SetState SetGlobalState(
            IDictionary<string, object> contextData,
            string key)
        {
            return new SetState(value =>
            {
                contextData[key] = value;
            });
        }

        [Obsolete]
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

        public static TContextData GetScopedState<TContextData>(
            IReadOnlyDictionary<string, object> contextData,
            string key) =>
            GetScopedStateWithDefault<TContextData>(contextData, key, false, default);

        public static TContextData GetScopedStateWithDefault<TContextData>(
            IReadOnlyDictionary<string, object> contextData,
            string key,
            bool hasDefaultValue,
            TContextData defaultValue)
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
            else if (hasDefaultValue)
            {
                return defaultValue;
            }

            // TODO : resources
            throw new ArgumentException(
                $"The specified key `{key}` does not exist on `context.ScopedContextData`.");
        }

        public static SetState<TContextData> SetScopedStateGeneric<TContextData>(
            IResolverContext context,
            string key)
        {
            return new SetState<TContextData>(value =>
            {
                context.ScopedContextData = context.ScopedContextData.SetItem(key ,value);
            });
        }

        public static SetState SetScopedState(
            IResolverContext context,
            string key)
        {
            return new SetState(value =>
            {
                context.ScopedContextData = context.ScopedContextData.SetItem(key ,value);
            });
        }
    }
}
