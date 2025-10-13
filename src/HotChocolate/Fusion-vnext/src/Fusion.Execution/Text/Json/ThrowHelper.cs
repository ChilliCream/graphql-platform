using System.Diagnostics.CodeAnalysis;
using static HotChocolate.Fusion.Properties.FusionExecutionResources;

namespace HotChocolate.Fusion.Text.Json;

internal static class ThrowHelper
{
    public static FormatException FormatException()
    {
        return new FormatException { Source = Rethrowable };
    }

    [DoesNotReturn]
    public static void ThrowInvalidOperationException_ReadInvalidUTF16(int charAsInt)
    {
        throw new InvalidOperationException(string.Format(
            ThrowHelper_ReadInvalidUTF16,
            $"0x{charAsInt:X2}"))
        {
            Source = Rethrowable
        };
    }

    [DoesNotReturn]
    public static void ThrowInvalidOperationException_ReadIncompleteUTF16()
    {
        throw new InvalidOperationException(ThrowHelper_ReadIncompleteUTF16)
        {
            Source = Rethrowable
        };
    }
}
