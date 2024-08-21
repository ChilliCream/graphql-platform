using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
#if NET8_0_OR_GREATER
using GreenDonut.Projections;
#endif

namespace GreenDonut;

public readonly struct DataLoaderFetchContext<TValue>
{
    public DataLoaderFetchContext(IImmutableDictionary<string, object?> contextData)
    {
        ContextData = contextData;
    }

    public IImmutableDictionary<string, object?> ContextData { get; }

    public TState? GetState<TState>(string key)
    {
        if (ContextData.TryGetValue(key, out var value) && value is TState state)
        {
            return state;
        }

        return default;
    }

    public TState GetRequiredState<TState>(string key)
    {
        if (ContextData.TryGetValue(key, out var value) && value is TState state)
        {
            return state;
        }

        throw new InvalidOperationException(
            $"The state `{key}` is not available on the DataLoader.");
    }

    public TState GetStateOrDefault<TState>(string key, TState defaultValue)
    {
        if (ContextData.TryGetValue(key, out var value) && value is TState state)
        {
            return state;
        }

        return defaultValue;
    }
#if NET8_0_OR_GREATER

    [Experimental(Experiments.Projections)]
    public ISelectorBuilder GetSelector()
    {
        DefaultSelectorBuilder<TValue> context;
        if (ContextData.TryGetValue(typeof(ISelectorBuilder).FullName!, out var value)
            && value is DefaultSelectorBuilder<TValue> casted)
        {
            context = casted;
        }
        else
        {
            context = new DefaultSelectorBuilder<TValue>();
        }

        return context;
    }
#endif
}
