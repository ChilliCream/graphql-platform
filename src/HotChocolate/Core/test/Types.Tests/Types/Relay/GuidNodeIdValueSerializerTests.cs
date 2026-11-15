using System.Text;

namespace HotChocolate.Types.Relay;

public class GuidNodeIdValueSerializerTests
{
    private static readonly Guid s_guid = new("1ae27b14-8cf6-440d-9a46-09090a4af6f3");

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TryParse_Should_ReturnGuid_When_BufferIsRawBytes(bool compress)
    {
        // arrange
        var serializer = new GuidNodeIdValueSerializer(compress);

        // act
        var success = serializer.TryParse(s_guid.ToByteArray(), out var value);

        // assert
        Assert.True(success);
        Assert.Equal(s_guid, value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TryParse_Should_ReturnGuid_When_BufferIsHex(bool compress)
    {
        // arrange
        var serializer = new GuidNodeIdValueSerializer(compress);

        // act
        var success = serializer.TryParse("1ae27b148cf6440d9a4609090a4af6f3"u8, out var value);

        // assert
        Assert.True(success);
        Assert.Equal(s_guid, value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TryParse_Should_ReturnEmptyGuid_When_BufferIsRawEmptyGuid(bool compress)
    {
        // arrange
        var serializer = new GuidNodeIdValueSerializer(compress);

        // act
        var success = serializer.TryParse(new byte[16], out var value);

        // assert
        Assert.True(success);
        Assert.Equal(Guid.Empty, value);
    }

    [Fact]
    public void TryParse_Should_ReturnEmptyGuid_When_BufferIsEmptyAndCompressIsTrue()
    {
        // arrange
        var serializer = new GuidNodeIdValueSerializer(compress: true);

        // act
        var success = serializer.TryParse([], out var value);

        // assert
        Assert.True(success);
        Assert.Equal(Guid.Empty, value);
    }

    [Fact]
    public void TryParse_Should_ReturnFalse_When_BufferIsEmptyAndCompressIsFalse()
    {
        // arrange
        var serializer = new GuidNodeIdValueSerializer(compress: false);

        // act
        var success = serializer.TryParse([], out var value);

        // assert
        Assert.False(success);
        Assert.Null(value);
    }

    [Theory]
    [InlineData(true, 15)]
    [InlineData(false, 15)]
    [InlineData(true, 17)]
    [InlineData(false, 17)]
    public void TryParse_Should_ReturnFalse_When_BufferHasNoGuidLength(bool compress, int length)
    {
        // arrange
        var serializer = new GuidNodeIdValueSerializer(compress);

        // act
        var success = serializer.TryParse(Encoding.UTF8.GetBytes(new string('a', length)), out var value);

        // assert
        Assert.False(success);
        Assert.Null(value);
    }
}
