namespace HotChocolate.Buffers;

internal interface IJsonSegmentSource
{
    ReadOnlySpan<byte> Read(ref int start, ref int length);

    bool SequenceEqual(int locationA, int locationB, int length);

    int GetHashCode(int location, int length);
}
