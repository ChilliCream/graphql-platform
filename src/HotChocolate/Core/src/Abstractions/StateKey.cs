using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate;

public readonly struct StateKey<T>
{
    public readonly string Key;

    // Don't expose this constructor. Create is a deliberate abstraction.
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

public readonly struct StateFlagKey
{
    public readonly string Key;

    // Don't expose this constructor. Create is a deliberate abstraction.
    internal StateFlagKey(string key)
    {
        Key = key;
    }

    public static implicit operator string(StateFlagKey key) => key.Key;

    public static StateFlagKey Create(string key) => new(key);
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

    public static bool Has(
        this IReadOnlyDictionary<string, object?> contextData,
        StateFlagKey key)
    {
        return contextData.ContainsKey(key);
    }

    public static void Add(
        this IDictionary<string, object?> contextData,
        StateFlagKey key)
    {
        contextData[key] = null;
    }
}

