using HotChocolate.Utilities;
using static HotChocolate.Properties.TypeResources;

namespace HotChocolate.Resolvers.Expressions;

internal static class ExpressionHelper
{
    public static async ValueTask<object> AwaitTaskHelper<T>(Task<T> task)
    {
        if (task is null)
        {
            return null;
        }
        return await task.ConfigureAwait(false);
    }

    public static async ValueTask<object> AwaitValueTaskHelper<T>(ValueTask<T> task)
        => await task.ConfigureAwait(false);

    public static ValueTask<object> WrapResultHelper<T>(T result)
        => new(result);

    public static TContextData GetGlobalState<TContextData>(
        IDictionary<string, object> contextData,
        string key,
        bool defaultIfNotExists = false)
        => GetGlobalStateWithDefault<TContextData>(contextData, key, defaultIfNotExists, default);

    public static TContextData GetGlobalStateWithDefault<TContextData>(
        IDictionary<string, object> contextData,
        string key,
        bool hasDefaultValue,
        TContextData defaultValue)
    {
        if (contextData.TryGetValue(key, out var value))
        {
            if (ReferenceEquals(value, null))
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

        throw new ArgumentException(
            string.Format(ExpressionHelper_GetGlobalStateWithDefault_NoDefaults, key));
    }

    public static SetState<TContextData> SetGlobalStateGeneric<TContextData>(
        IDictionary<string, object> contextData,
        string key)
        => value => contextData[key] = value;

    public static SetState SetGlobalState(
        IDictionary<string, object> contextData,
        string key)
        => value => contextData[key] = value;

    public static TContextData GetScopedState<TContextData>(
        IResolverContext context,
        IReadOnlyDictionary<string, object> contextData,
        string key,
        bool defaultIfNotExists = false)
        => GetScopedStateWithDefault<TContextData>(
            context, contextData, key, defaultIfNotExists, default);

    public static TContextData GetScopedStateWithDefault<TContextData>(
        IResolverContext context,
        IReadOnlyDictionary<string, object> contextData,
        string key,
        bool hasDefaultValue,
        TContextData defaultValue)
    {
        if (contextData.TryGetValue(key, out var value))
        {
            if (value is null)
            {
                return default;
            }

            if (value is TContextData v ||
                context.Service<ITypeConverter>().TryConvert(value, out v))
            {
                return v;
            }
        }
        else if (hasDefaultValue)
        {
            return defaultValue;
        }

        throw new ArgumentException(
            string.Format(ExpressionHelper_GetScopedStateWithDefault_NoDefaultValue, key));
    }

    public static SetState<TContextData> SetScopedStateGeneric<TContextData>(
        IResolverContext context,
        string key)
        => value => context.ScopedContextData = context.ScopedContextData.SetItem(key, value);

    public static SetState SetScopedState(
        IResolverContext context,
        string key)
        => value => context.ScopedContextData = context.ScopedContextData.SetItem(key, value);

    public static SetState<TContextData> SetLocalStateGeneric<TContextData>(
        IResolverContext context,
        string key)
        => value => context.LocalContextData = context.LocalContextData.SetItem(key, value);

    public static SetState SetLocalState(
        IResolverContext context,
        string key)
        => value => context.LocalContextData = context.LocalContextData.SetItem(key, value);
}
