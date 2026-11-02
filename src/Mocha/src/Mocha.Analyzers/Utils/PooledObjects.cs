using System.Text;

namespace Mocha.Analyzers.Utils;

/// <summary>
/// Provides a thread-safe object pool for reusable <see cref="StringBuilder"/>,
/// <see cref="HashSet{T}"/>, and <see cref="Dictionary{TKey, TValue}"/> instances
/// to reduce allocations during source generation.
/// </summary>
public static class PooledObjects
{
    private static readonly HashSet<string>?[] s_stringSets = new HashSet<string>[8];
    private static int s_nextStringSetIndex = -1;

    private static readonly StringBuilder?[] s_stringBuilders = new StringBuilder[8];
    private static int s_nextStringBuilderIndex = -1;

    private static readonly Dictionary<string, string>?[] s_stringDictionaries = new Dictionary<string, string>[8];
    private static int s_nextStringDictionaryIndex = -1;

    private static readonly object s_lock = new();

    /// <summary>
    /// Gets a <see cref="StringBuilder"/> from the pool, or creates a new one if the pool is empty.
    /// </summary>
    /// <returns>A cleared <see cref="StringBuilder"/> ready for use.</returns>
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

    /// <summary>
    /// Gets a <see cref="HashSet{T}"/> of strings from the pool, or creates a new one if the pool is empty.
    /// </summary>
    /// <returns>A cleared <see cref="HashSet{T}"/> ready for use.</returns>
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

    /// <summary>
    /// Gets a <see cref="Dictionary{TKey, TValue}"/> of strings from the pool, or creates a new one if the pool is empty.
    /// </summary>
    /// <returns>A cleared <see cref="Dictionary{TKey, TValue}"/> ready for use.</returns>
    public static Dictionary<string, string> GetStringDictionary()
    {
        lock (s_lock)
        {
            if (s_nextStringDictionaryIndex >= 0)
            {
                var dict = s_stringDictionaries[s_nextStringDictionaryIndex];
                s_stringDictionaries[s_nextStringDictionaryIndex] = null;
                s_nextStringDictionaryIndex--;
                return dict ?? new Dictionary<string, string>();
            }
        }

        return new Dictionary<string, string>();
    }

    /// <summary>
    /// Returns a <see cref="StringBuilder"/> to the pool for reuse.
    /// </summary>
    /// <param name="stringBuilder">The string builder to return.</param>
    /// <remarks>
    /// Instances whose capacity exceeds 128 KB are discarded to avoid retaining excessive memory.
    /// </remarks>
    public static void Return(StringBuilder stringBuilder)
    {
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

    /// <summary>
    /// Returns a <see cref="HashSet{T}"/> of strings to the pool for reuse.
    /// </summary>
    /// <param name="stringSet">The hash set to return.</param>
    /// <remarks>
    /// Instances containing more than 256 entries are discarded to avoid retaining excessive memory.
    /// </remarks>
    public static void Return(HashSet<string> stringSet)
    {
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

    /// <summary>
    /// Returns a <see cref="Dictionary{TKey, TValue}"/> of strings to the pool for reuse.
    /// </summary>
    /// <param name="stringDictionary">The dictionary to return.</param>
    /// <remarks>
    /// Instances containing more than 256 entries are discarded to avoid retaining excessive memory.
    /// </remarks>
    public static void Return(Dictionary<string, string> stringDictionary)
    {
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
