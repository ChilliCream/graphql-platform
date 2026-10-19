using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

#if FUSION
using static HotChocolate.Fusion.Properties.FusionExecutionResources;

namespace HotChocolate.Fusion.Text.Json;
#else
using static HotChocolate.Properties.TextJsonResources;

namespace HotChocolate.Text.Json;
#endif

internal static class ThrowHelper
{
    public static InvalidOperationException SourceResultDocumentTooLarge()
        => new("The source result document has exceeded its maximum data capacity.");

    public static JsonException InvalidSourceJson(ReadOnlySpan<byte> data, int position)
        => position < data.Length
            ? new JsonException(
                $"The JSON input contains the unexpected byte 0x{data[position]:X2} at byte position {position}.",
                path: null,
                lineNumber: null,
                bytePositionInLine: position)
            : new JsonException(
                $"The JSON input ends unexpectedly at byte position {position}.",
                path: null,
                lineNumber: null,
                bytePositionInLine: position);

    public static JsonException SourceJsonDepthExceeded(int maxDepth, int position)
        => new(
            $"The JSON input exceeds the maximum depth of {maxDepth} at byte position {position}.",
            path: null,
            lineNumber: null,
            bytePositionInLine: position);

    public static FormatException FormatException()
    {
        return new FormatException { Source = Rethrowable };
    }

    [DoesNotReturn]
    public static void ThrowArgumentException_ValueTooLarge(int length)
    {
        throw new ArgumentException(
            string.Format(
                "The JSON value of length {0} is too large and not supported.",
                length),
            "value");
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
