namespace HotChocolate.Transport.Formatters;

/// <summary>
/// An internal helper class that centralizes the exceptions of the formatters.
/// </summary>
internal static class ThrowHelper
{
    public static ArgumentOutOfRangeException ChunkedPipeBufferWriter_AdvanceNegative(int bytes)
        => new(nameof(bytes), bytes, "The number of bytes to advance must not be negative.");

    public static InvalidOperationException ChunkedPipeBufferWriter_AdvancePastBuffer()
        => new("Cannot advance past the end of the buffer.");
}
