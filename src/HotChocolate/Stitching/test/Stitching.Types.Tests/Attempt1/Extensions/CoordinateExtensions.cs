using System.Runtime.CompilerServices;

namespace HotChocolate.Stitching.Types.Attempt1;

internal static class CoordinateExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEqualTo(this ISchemaCoordinate2? x, ISchemaCoordinate2? y)
    {
        if (x is null)
        {
            return y is null;
        }

        if (y is null)
        {
            return false;
        }

        return x.Equals(y);
    }
}
