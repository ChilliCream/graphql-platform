using System.Text;

namespace HotChocolate.Types.Analyzers.Helpers;

public static class PooledObjects
{
    private static readonly HashSet<string>?[] _stringSets = new HashSet<string>[8];
    private static int _nextStringSetIndex = -1;

    private static readonly StringBuilder?[] _stringBuilders = new StringBuilder[8];
    private static int _nextStringBuilderIndex = -1;

    private static readonly object _lock = new();

    public static StringBuilder GetStringBuilder()
    {
        lock (_lock)
        {
            if (_nextStringBuilderIndex >= 0)
            {
                var sb = _stringBuilders[_nextStringBuilderIndex];
                _stringBuilders[_nextStringBuilderIndex] = null;
                _nextStringBuilderIndex--;
                return sb ?? new StringBuilder();
            }
        }

        return new StringBuilder();
    }

    public static HashSet<string> GetStringSet()
    {
        lock (_lock)
        {
            if (_nextStringSetIndex >= 0)
            {
                var set = _stringSets[_nextStringSetIndex];
                _stringSets[_nextStringSetIndex] = null;
                _nextStringSetIndex--;
                return set ?? [];
            }
        }

        return [];
    }

    public static void Return(StringBuilder stringBuilder)
    {
        stringBuilder.Clear();

        lock (_lock)
        {
            if (_nextStringBuilderIndex + 1 < _stringBuilders.Length)
            {
                _nextStringBuilderIndex++;
                _stringBuilders[_nextStringBuilderIndex] = stringBuilder;
            }
        }
    }

    public static void Return(HashSet<string> stringSet)
    {
        stringSet.Clear();

        lock (_lock)
        {
            if (_nextStringSetIndex + 1 < _stringSets.Length)
            {
                _nextStringSetIndex++;
                _stringSets[_nextStringSetIndex] = stringSet;
            }
        }
    }
}
