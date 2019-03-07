using System.Collections.Generic;

namespace Generator
{
    internal static class DictionaryExtensions
    {
        internal static T TryGet<T>(this Dictionary<object, object> dictionary, string key, T defaultValue)
            where T : class
        {
            if (dictionary.ContainsKey(key))
            {
                return dictionary[key] as T;
            }

            return defaultValue;
        }

        internal static bool TryGet(this Dictionary<object, object> dictionary, string key, bool defaultValue)
        {
            if (dictionary.ContainsKey(key))
            {
                return bool.Parse(dictionary[key] as string);
            }

            return defaultValue;
        }

        internal static int TryGet(this Dictionary<object, object> dictionary, string key, int defaultValue)
        {
            if (dictionary.ContainsKey(key))
            {
                return int.Parse(dictionary[key] as string);
            }

            return defaultValue;
        }

        internal static bool ContainsKeys(this Dictionary<object, object> dictionary, params string[] keys)
        {
            var result = false;

            foreach (var key in keys)
            {
                result |= dictionary.ContainsKey(key);
            }

            return result;
        }
    }
}
