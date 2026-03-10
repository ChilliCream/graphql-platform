using System.Collections.Concurrent;
using System.Text;

namespace HotChocolate.Types.Analyzers.Helpers;

public static class PooledObjects
{
    private static readonly HashSet<string>?[] s_stringSets = new HashSet<string>[8];
    private static int s_nextStringSetIndex = -1;

    private static readonly StringBuilder?[] s_stringBuilders = new StringBuilder[8];
    private static int s_nextStringBuilderIndex = -1;

    private static readonly ConcurrentDictionary<string, string>?[] s_stringDictionaries =
        new ConcurrentDictionary<string, string>[8];
    private static int s_nextStringDictionaryIndex = -1;

    private static readonly object s_lock = new();

    public static StringBuilder GetStringBuilder()
    {
        lock (s_lock)
        {
            if (s_nextStringBuilderIndex >= 0)
            {
                var sb = s_stringBuilders[s_nextStringBuilderIndex];
                s_stringBuilders[s_nextStringBuilderIndex] = null;
                s_nextStringBuilderIndex--;
                return sb ?? new StringBuilder();
            }
        }

        return new StringBuilder();
    }

    public static HashSet<string> GetStringSet()
    {
        lock (s_lock)
        {
            if (s_nextStringSetIndex >= 0)
            {
                var set = s_stringSets[s_nextStringSetIndex];
                s_stringSets[s_nextStringSetIndex] = null;
                s_nextStringSetIndex--;
                return set ?? [];
            }
        }

        return [];
    }

    public static ConcurrentDictionary<string, string> GetStringDictionary()
    {
        lock (s_lock)
        {
            if (s_nextStringDictionaryIndex >= 0)
            {
                var dict = s_stringDictionaries[s_nextStringDictionaryIndex];
                s_stringDictionaries[s_nextStringDictionaryIndex] = null;
                s_nextStringDictionaryIndex--;
                return dict ?? new ConcurrentDictionary<string, string>();
            }
        }

        return new ConcurrentDictionary<string, string>();
    }

    public static void Return(StringBuilder stringBuilder)
    {
        // Don't pool oversized objects to avoid memory bloat
        // 128KB should accommodate most generated files while protecting against truly massive outliers
        const int maxCapacity = 128 * 1024;
        if (stringBuilder.Capacity > maxCapacity)
        {
            return;
        }

        lock (s_lock)
        {
            stringBuilder.Clear();

            if (s_nextStringBuilderIndex + 1 < s_stringBuilders.Length)
            {
                s_nextStringBuilderIndex++;
                s_stringBuilders[s_nextStringBuilderIndex] = stringBuilder;
            }
        }
    }

    public static void Return(HashSet<string> stringSet)
    {
        // Don't pool oversized objects to avoid memory bloat
        const int maxCount = 256;
        if (stringSet.Count > maxCount)
        {
            return;
        }

        lock (s_lock)
        {
            stringSet.Clear();

            if (s_nextStringSetIndex + 1 < s_stringSets.Length)
            {
                s_nextStringSetIndex++;
                s_stringSets[s_nextStringSetIndex] = stringSet;
            }
        }
    }

    public static void Return(ConcurrentDictionary<string, string> stringDictionary)
    {
        // Don't pool oversized objects to avoid memory bloat
        const int maxCount = 256;
        if (stringDictionary.Count > maxCount)
        {
            return;
        }

        lock (s_lock)
        {
            stringDictionary.Clear();

            if (s_nextStringDictionaryIndex + 1 < s_stringDictionaries.Length)
            {
                s_nextStringDictionaryIndex++;
                s_stringDictionaries[s_nextStringDictionaryIndex] = stringDictionary;
            }
        }
    }
}
