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

#if FUSION
    public static InvalidOperationException TooManySourceDocuments()
        => new("The result has more source documents than a result document can reference.");
#endif

    public static JsonException InvalidSourceJson(ReadOnlySpan<byte> data, int position, int offset)
        => new(
            position < data.Length
                ? $"The JSON input contains the unexpected byte 0x{data[position]:X2} at byte position {offset + position}."
                : $"The JSON input ends unexpectedly at byte position {offset + position}.");

    public static JsonException SourceJsonDepthExceeded(int maxDepth, int position)
        => new($"The JSON input exceeds the maximum depth of {maxDepth} at byte position {position}.");

    public static ArgumentException ExpectedFieldsNeedValueNodes()
        => new("Every expected field of a source result object needs the node of its value.");

    public static ArgumentException ExpectedFieldNamesAreNotDistinct(string name)
        => new($"The expected field '{name}' occurs more than once in an identified source result object.");

    public static ArgumentException ExpectedFieldNameIsNotPlain(string name)
        => new($"The expected field name '{name}' must consist of ASCII letters, digits and underscores.");

    public static FormatException FormatException()
    {
        return new FormatException { Source = Rethrowable };
    }

    public static InvalidOperationException SourceElementIsNotAnObject()
        => new();

    public static InvalidOperationException SourceElementIsNotAnArray(JsonTokenType tokenType)
        => new(string.Format(
            "The requested operation requires an element of type '{0}', but the target element has type '{1}'.",
            JsonTokenType.StartArray,
            tokenType))
        {
            Source = Rethrowable
        };

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
    public static void ThrowInvalidOperationException_MaxDepthExceeded()
        => throw new InvalidOperationException("The JSON depth is exceeds the max allowed depth.");

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
