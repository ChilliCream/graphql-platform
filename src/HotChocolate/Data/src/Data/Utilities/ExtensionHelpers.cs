using System.Collections.Generic;

namespace HotChocolate.Data.Utilities
{
    internal static class ExtensionHelpers
    {
        public static void MergeListDictionary<TKey, TValue>(
            IDictionary<TKey, List<TValue>> from,
            IDictionary<TKey, List<TValue>> to)
            where TKey : notnull
        {
            foreach (KeyValuePair<TKey, List<TValue>> element in from)
            {
                if (to.TryGetValue(element.Key, out var configurations))
                {
                    configurations.AddRange(element.Value);
                }
                else
                {
                    to[element.Key] = element.Value;
                }
            }
        }

        public static void MergeDictionary<TKey, TValue>(
            IDictionary<TKey, TValue> from,
            IDictionary<TKey, TValue> to)
            where TKey : notnull
        {
            foreach (KeyValuePair<TKey, TValue> element in from)
            {
                to[element.Key] = element.Value;
            }
        }
    }
}
