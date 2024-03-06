using System.Text;

namespace HotChocolate.Types.Analyzers.Helpers;

public static class StringBuilderPool
{
    private static StringBuilder? _stringBuilder;

    public static StringBuilder Get()
    {
        var stringBuilder = Interlocked.Exchange(ref _stringBuilder, null);
        return stringBuilder ?? new StringBuilder();
    }

    public static void Return(StringBuilder stringBuilder)
    {
        stringBuilder.Clear();
        Interlocked.CompareExchange(ref _stringBuilder, stringBuilder, null);
    }
}
