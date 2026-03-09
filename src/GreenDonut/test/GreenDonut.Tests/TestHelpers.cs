namespace GreenDonut;

internal static class TestHelpers
{
    public static FetchDataDelegate<TKey, TValue> CreateFetch<TKey, TValue>()
        where TKey : notnull
    {
        return (_, _, _) => default;
    }

    public static FetchDataDelegate<TKey, TValue> CreateFetch<TKey, TValue>(Exception error)
        where TKey : notnull
    {
        return (_, results, _) =>
        {
            results.Span[0] = error;
            return default;
        };
    }

    public static FetchDataDelegate<TKey, TValue> CreateFetch<TKey, TValue>(Result<TValue?> value)
        where TKey : notnull
    {
        return (_, results, _) =>
        {
            results.Span[0] = value;
            return default;
        };
    }

    public static FetchDataDelegate<TKey, TValue> CreateFetch<TKey, TValue>(Result<TValue?>[] values)
        where TKey : notnull
    {
        return (_, results, _) =>
        {
            var span = results.Span;

            for (var i = 0; i < results.Length; i++)
            {
                span[i] = values[i];
            }

            return default;
        };
    }
}
