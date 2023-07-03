using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate;

public readonly struct StateKey<T>
{
    public readonly string Key;

    internal StateKey(string key)
    {
        Key = key;
    }

    public static implicit operator string(StateKey<T> key) => key.Key;
}

public static class StateKey
{
    public static StateKey<T> Create<T>(string key) => new(key);
}

public static class ContextDataExtensions
{
    public static bool TryGet<T>(
        this IReadOnlyDictionary<string, object?> contextData,
        StateKey<T> key,
        [NotNullWhen(true)] out T? value)
    {
        if (contextData.TryGetValue(key, out var obj))
        {
            value = (T) obj!;
            return true;
        }

        value = default;
        return false;
    }

    public static void Set<T>(
        this IDictionary<string, object?> contextData,
        StateKey<T> key,
        T value)
    {
        contextData[key] = value;
    }
}
