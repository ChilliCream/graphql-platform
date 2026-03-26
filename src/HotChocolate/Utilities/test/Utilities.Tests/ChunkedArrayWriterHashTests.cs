using System.Buffers;
using HotChocolate.Buffers;

namespace HotChocolate.Utilities;

public class ChunkedArrayWriterHashTests
{
    /// <summary>
    /// Reference scalar implementation for verification.
    /// </summary>
    private static int ScalarHash(byte[] data)
    {
        var hash = 0u;

        foreach (var b in data)
        {
            hash = hash * 31 + b;
        }

        return (int)(hash & 0x7FFFFFFF);
    }

    private static ChunkedArrayWriter CreateWriterWithData(byte[] data)
    {
        var writer = new ChunkedArrayWriter();
        var span = writer.GetSpan(data.Length);
        data.CopyTo(span);
        writer.Advance(data.Length);
        return writer;
    }

    [Fact]
    public void GetHashCode_EmptySegment_ReturnsZero()
    {
        using var writer = CreateWriterWithData([1, 2, 3]);

        Assert.Equal(0, writer.GetHashCode(0, 0));
    }

    [Fact]
    public void GetHashCode_SingleByte_MatchesScalar()
    {
        byte[] data = [42];
        using var writer = CreateWriterWithData(data);

        Assert.Equal(ScalarHash(data), writer.GetHashCode(0, data.Length));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(31)]
    [InlineData(32)]
    [InlineData(33)]
    [InlineData(63)]
    [InlineData(64)]
    [InlineData(65)]
    [InlineData(100)]
    [InlineData(256)]
    [InlineData(500)]
    public void GetHashCode_VariousLengths_MatchesScalar(int length)
    {
        var data = new byte[length];
        var rng = new Random(42);
        rng.NextBytes(data);

        using var writer = CreateWriterWithData(data);

        Assert.Equal(ScalarHash(data), writer.GetHashCode(0, data.Length));
    }

    [Fact]
    public void GetHashCode_AllZeros_MatchesScalar()
    {
        var data = new byte[128];
        using var writer = CreateWriterWithData(data);

        Assert.Equal(ScalarHash(data), writer.GetHashCode(0, data.Length));
    }

    [Fact]
    public void GetHashCode_AllOnes_MatchesScalar()
    {
        var data = new byte[128];
        Array.Fill(data, (byte)0xFF);
        using var writer = CreateWriterWithData(data);

        Assert.Equal(ScalarHash(data), writer.GetHashCode(0, data.Length));
    }

    [Fact]
    public void GetHashCode_SequentialBytes_MatchesScalar()
    {
        var data = new byte[256];

        for (var i = 0; i < data.Length; i++)
        {
            data[i] = (byte)(i & 0xFF);
        }

        using var writer = CreateWriterWithData(data);

        Assert.Equal(ScalarHash(data), writer.GetHashCode(0, data.Length));
    }

    [Fact]
    public void GetHashCode_OffsetIntoData_MatchesScalar()
    {
        var data = new byte[200];
        var rng = new Random(99);
        rng.NextBytes(data);

        using var writer = CreateWriterWithData(data);

        // Hash a sub-range starting at offset 50, length 100
        var expected = ScalarHash(data[50..150]);
        Assert.Equal(expected, writer.GetHashCode(50, 100));
    }

    [Fact]
    public void GetHashCode_ConsistentAcrossMultipleCalls()
    {
        var data = new byte[128];
        var rng = new Random(7);
        rng.NextBytes(data);

        using var writer = CreateWriterWithData(data);

        var hash1 = writer.GetHashCode(0, data.Length);
        var hash2 = writer.GetHashCode(0, data.Length);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_DifferentData_DifferentHashes()
    {
        var data1 = new byte[64];
        var data2 = new byte[64];
        Array.Fill(data1, (byte)1);
        Array.Fill(data2, (byte)2);

        using var writer1 = CreateWriterWithData(data1);
        using var writer2 = CreateWriterWithData(data2);

        Assert.NotEqual(
            writer1.GetHashCode(0, data1.Length),
            writer2.GetHashCode(0, data2.Length));
    }

    [Fact]
    public void GetHashCode_ResultIsNonNegative()
    {
        var data = new byte[256];
        var rng = new Random(13);
        rng.NextBytes(data);

        using var writer = CreateWriterWithData(data);

        Assert.True(writer.GetHashCode(0, data.Length) >= 0);
    }
}
