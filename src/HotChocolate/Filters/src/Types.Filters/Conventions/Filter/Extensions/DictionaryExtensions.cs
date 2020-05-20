using System;
using System.Collections.Generic;

namespace HotChocolate.Types.Filters.Extensions
{
    public static class DictionaryExtensions
    {
        public static TValue GetOrAdd<TKey, TValue>(
            this IDictionary<TKey, TValue> dict,
            TKey key,
            Func<TKey, TValue> producer)
            where TKey : notnull
        {
            if (!dict.ContainsKey(key))
            {
                dict[key] = producer(key);
            }
            return dict[key];
        }
    }
}
