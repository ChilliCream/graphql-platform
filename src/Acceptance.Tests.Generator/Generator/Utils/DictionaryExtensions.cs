using System.Collections.Generic;
using System.Linq;

namespace Generator
{
    internal static class DictionaryExtensions
    {
        internal static T TryGet<TKey, T>(this Dictionary<TKey, object> dictionary, TKey key, T defaultValue)
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

        internal static bool ContainsAdditionalKeysExcept(this Dictionary<object, object> dictionary, out string value, params string[] keys)
        {
            foreach (KeyValuePair<object, object> pair in dictionary)
            {
                if (!keys.Contains(pair.Key))
                {
                    value = pair.Key as string;
                    return true;
                }
            }

            value = string.Empty;
            return false;
        }
    }
}
