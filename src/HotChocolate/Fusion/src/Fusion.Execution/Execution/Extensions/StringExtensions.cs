using System.IO.Hashing;

namespace HotChocolate.Fusion.Execution;

internal static class FusionStringExtensions
{
    public static ulong ComputeHash(this ReadOnlyMemory<byte> utf8Bytes)
        => XxHash64.HashToUInt64(utf8Bytes.Span);
}
