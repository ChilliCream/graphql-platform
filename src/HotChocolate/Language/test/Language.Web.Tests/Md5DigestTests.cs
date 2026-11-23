using System.Buffers;
using System.Security.Cryptography;

namespace HotChocolate.Language;

public class Md5DigestTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(55)]
    [InlineData(56)]
    [InlineData(63)]
    [InlineData(64)]
    [InlineData(65)]
    [InlineData(119)]
    [InlineData(120)]
    [InlineData(128)]
    [InlineData(1000)]
    public void Compute_Should_MatchPlatformDigest_When_SourceIsContiguous(int length)
    {
        // arrange
        var source = CreateSource(length);
        var expected = MD5.HashData(source);
        var actual = new byte[Md5Digest.SizeInBytes];

        // act
        Md5Digest.Compute(source, actual);

        // assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(64, 63)]
    [InlineData(128, 64)]
    [InlineData(130, 65)]
    [InlineData(200, 1)]
    [InlineData(200, 7)]
    [InlineData(1000, 63)]
    [InlineData(1000, 65)]
    [InlineData(1000, 500)]
    public void Compute_Should_MatchPlatformDigest_When_SourceHasSeveralSegments(
        int length,
        int segmentLength)
    {
        // arrange
        var source = CreateSource(length);
        var expected = MD5.HashData(source);
        var actual = new byte[Md5Digest.SizeInBytes];
        var sequence = CreateSequence(source, segmentLength);

        // act
        Md5Digest.Compute(sequence, actual);

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Compute_Should_MatchPlatformDigest_When_SourceHasAnyLengthUpTo300()
    {
        // arrange
        var source = CreateSource(300);
        var actual = new byte[Md5Digest.SizeInBytes];
        var mismatches = new List<int>();

        // act
        for (var length = 0; length <= source.Length; length++)
        {
            var expected = MD5.HashData(source.AsSpan(0, length));
            Md5Digest.Compute(source.AsSpan(0, length), actual);

            if (!expected.AsSpan().SequenceEqual(actual))
            {
                mismatches.Add(length);
            }
        }

        // assert
        Assert.Empty(mismatches);
    }

    [Fact]
    public void Compute_Should_MatchPlatformDigest_When_SourceIsSplitAtRandomPositions()
    {
        // arrange
        var random = new Random(4711);
        var actual = new byte[Md5Digest.SizeInBytes];
        var mismatches = new List<int>();

        // act
        for (var round = 0; round < 2000; round++)
        {
            var source = CreateSource(random.Next(0, 601));
            var sequence = CreateSequence(source, random);
            var expected = MD5.HashData(source);
            Md5Digest.Compute(sequence, actual);

            if (!expected.AsSpan().SequenceEqual(actual))
            {
                mismatches.Add(round);
            }
        }

        // assert
        Assert.Empty(mismatches);
    }

    private static byte[] CreateSource(int length)
    {
        var source = new byte[length];
        new Random(length).NextBytes(source);
        return source;
    }

    // Segments are 0 to 130 bytes long.
    private static ReadOnlySequence<byte> CreateSequence(byte[] source, Random random)
    {
        var first = new Segment(source.AsMemory(0, Math.Min(random.Next(0, 131), source.Length)), 0);
        var last = first;
        var start = first.Memory.Length;

        while (start < source.Length)
        {
            var length = Math.Min(random.Next(0, 131), source.Length - start);
            last = last.Append(source.AsMemory(start, length));
            start += length;
        }

        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    private static ReadOnlySequence<byte> CreateSequence(byte[] source, int segmentLength)
    {
        var first = new Segment(source.AsMemory(0, Math.Min(segmentLength, source.Length)), 0);
        var last = first;

        for (var start = segmentLength; start < source.Length; start += segmentLength)
        {
            var length = Math.Min(segmentLength, source.Length - start);
            last = last.Append(source.AsMemory(start, length));
        }

        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    private sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory, long runningIndex)
        {
            Memory = memory;
            RunningIndex = runningIndex;
        }

        public Segment Append(ReadOnlyMemory<byte> memory)
        {
            var segment = new Segment(memory, RunningIndex + Memory.Length);
            Next = segment;
            return segment;
        }
    }
}
