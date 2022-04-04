using System.Runtime.CompilerServices;

namespace HotChocolate.Language;

internal static class HashCodeExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetHashCodeOrDefault<T>(this T? obj)
    {
        return obj is not null ? obj.GetHashCode() : 0;
    }
}
