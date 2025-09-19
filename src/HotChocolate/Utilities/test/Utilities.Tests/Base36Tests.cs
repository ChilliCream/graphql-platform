using System.Text;
using HotChocolate.Buffers.Text;

namespace HotChocolate.Utilities;

public class Base36Tests
{
    [Fact]
    public void Encode_EmptyInput_ReturnsEmptyString()
    {
        // Arrange
        var input = ReadOnlySpan<byte>.Empty;

        // Act
        var result = Base36.Encode(input);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Encode_SingleZeroByte_ReturnsZero()
    {
        // Arrange
        var input = new byte[] { 0 };

        // Act
        var result = Base36.Encode(input);

        // Assert
        Assert.Equal("0", result);
    }

    [Fact]
    public void Encode_SingleByte_ReturnsCorrectBase36()
    {
        // Arrange
        var input = new byte[] { 123 };

        // Act
        var result = Base36.Encode(input);

        // Assert
        Assert.Equal("3F", result);
    }

    [Fact]
    public void Encode_MultipleBytesProduceValidBase36()
    {
        // Arrange
        var input = "Hello"u8.ToArray();

        // Act
        var result = Base36.Encode(input);

        // Assert
        Assert.NotEmpty(result);
        Assert.True(result.All(c => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".Contains(c)));
    }

    [Theory]
    [InlineData("User:123", "1AS3ZUY1Q27UB")]
    [InlineData("Post:456", "1819ISLOI8PIU")]
    [InlineData("A", "1T")]
    [InlineData("Hello", "3YUD78MN")]
    [InlineData("", "")]
    public void Encode_CommonStrings_ReturnsExpectedBase36(string input, string expected)
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes(input);

        // Act
        var result = Base36.Encode(bytes);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Encode_LargeInput_ProducesValidBase36()
    {
        // Arrange - Create input larger than 16 bytes
        var input = "This is a very long string that will definitely exceed sixteen bytes"u8.ToArray();

        // Act
        var result = Base36.Encode(input);

        // Assert
        Assert.NotEmpty(result);
        Assert.True(result.All(c => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".Contains(c)));
    }

    [Fact]
    public void GetByteCount_EmptyInput_ReturnsZero()
    {
        // Arrange
        var input = ReadOnlySpan<char>.Empty;

        // Act
        var result = Base36.GetByteCount(input);

        // Assert
        Assert.Equal(0, result);
    }

    [Theory]
    [InlineData("0", 1)]
    [InlineData("3F", 1)]
    [InlineData("1T", 1)]
    [InlineData("3YUD78MN", 5)]
    public void GetByteCount_ValidInput_ReturnsReasonableEstimate(string input, int minimumExpected)
    {
        // Act
        var result = Base36.GetByteCount(input);

        // Assert
        Assert.True(result >= minimumExpected, $"Expected at least {minimumExpected} bytes, got {result}");
    }

    [Fact]
    public void Decode_EmptyInput_ReturnsZero()
    {
        // Arrange
        var input = ReadOnlySpan<char>.Empty;
        Span<byte> output = stackalloc byte[10];

        // Act
        var bytesWritten = Base36.Decode(input, output);

        // Assert
        Assert.Equal(0, bytesWritten);
    }

    [Fact]
    public void Decode_SingleZero_ReturnsZeroByte()
    {
        // Arrange
        var input = "0".AsSpan();
        Span<byte> output = stackalloc byte[10];

        // Act
        var bytesWritten = Base36.Decode(input, output);

        // Assert
        Assert.Equal(1, bytesWritten);
        Assert.Equal(0, output[0]);
    }

    [Theory]
    [InlineData("1AS3ZUY1Q27UB", "User:123")]
    [InlineData("1819ISLOI8PIU", "Post:456")]
    [InlineData("1T", "A")]
    [InlineData("3YUD78MN", "Hello")]
    [InlineData("", "")]
    public void Decode_ValidBase36_ReturnsOriginalString(string encoded, string expected)
    {
        // Arrange
        var bufferSize = Base36.GetByteCount(encoded);
        Span<byte> buffer = stackalloc byte[bufferSize];

        // Act
        var bytesWritten = Base36.Decode(encoded, buffer);
        var result = Encoding.UTF8.GetString(buffer[..bytesWritten]);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Decode_CaseInsensitive_ReturnsSameResult()
    {
        // Arrange
        const string upperCase = "3YUD78MN"; // "Hello" in uppercase
        const string lowerCase = "3yud78mn"; // "Hello" in lowercase
        var bufferSize = Math.Max(Base36.GetByteCount(upperCase), Base36.GetByteCount(lowerCase));
        Span<byte> buffer1 = stackalloc byte[bufferSize];
        Span<byte> buffer2 = stackalloc byte[bufferSize];

        // Act
        var bytes1 = Base36.Decode(upperCase, buffer1);
        var bytes2 = Base36.Decode(lowerCase, buffer2);

        // Assert
        Assert.Equal(bytes1, bytes2);
        Assert.True(buffer1[..bytes1].SequenceEqual(buffer2[..bytes2]));

        // Both should decode to "Hello"
        var result1 = Encoding.UTF8.GetString(buffer1[..bytes1]);
        var result2 = Encoding.UTF8.GetString(buffer2[..bytes2]);
        Assert.Equal("Hello", result1);
        Assert.Equal("Hello", result2);
    }

    [Fact]
    public void Decode_BufferTooSmall_ThrowsArgumentException()
    {
        // Arrange
        const string input = "3YUD78MN"; // "Hello" - 5 bytes
        var smallBuffer = new byte[3]; // Too small

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Base36.Decode(input, smallBuffer));
    }

    [Theory]
    [InlineData("!")]
    [InlineData("@")]
    [InlineData("~")]
    [InlineData("3YUD78M!")]
    public void Decode_InvalidCharacters_ThrowsArgumentException(string invalidInput)
    {
        // Arrange
        var buffer = new byte[100];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Base36.Decode(invalidInput, buffer));
    }

    [Fact]
    public void RoundTrip_LargeInput_MaintainsData()
    {
        // Arrange - Large input to test BigInteger path
        var original = Encoding.UTF8.GetBytes(
            "This is a very long GraphQL global identifier that exceeds sixteen bytes and "
            + "should work correctly with the mathematical BigInteger encoding approach");

        // Act
        var encoded = Base36.Encode(original);
        var bufferSize = Base36.GetByteCount(encoded);
        var decoded = new byte[bufferSize];
        var bytesWritten = Base36.Decode(encoded, decoded);

        // Assert
        Assert.True(original.AsSpan().SequenceEqual(decoded.AsSpan()[..bytesWritten]));
    }

    [Fact]
    public void RoundTrip_BinaryData_MaintainsData()
    {
        // Arrange
        var original = new byte[128];
        for (var i = 0; i < 128; i++)
        {
            original[i] = (byte)(i + 1);
        }

        // Act
        var encoded = Base36.Encode(original);
        var bufferSize = Base36.GetByteCount(encoded);
        var decoded = new byte[bufferSize];
        var bytesWritten = Base36.Decode(encoded, decoded);

        // Assert
        Assert.True(original.AsSpan().SequenceEqual(decoded.AsSpan()[..bytesWritten]));
    }

    [Fact]
    public void Encode_LeadingZeros_AreNotPreserved()
    {
        // Arrange
        var withLeadingZeros = "\0\0Hi"u8.ToArray(); // "\0\0Hi"
        var withoutLeadingZeros = "Hi"u8.ToArray(); // "Hi"

        // Act
        var encoded1 = Base36.Encode(withLeadingZeros);
        var encoded2 = Base36.Encode(withoutLeadingZeros);

        // Assert
        Assert.Equal(encoded1, encoded2);
    }

    [Fact]
    public void Encode_Output_ContainsOnlyValidBase36Characters()
    {
        // Arrange
        var input = "Any string input here!"u8.ToArray();
        const string validChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        // Act
        var result = Base36.Encode(input);

        // Assert
        Assert.True(result.All(c => validChars.Contains(c)),
            $"Result contains invalid characters: {result}");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(32)]
    [InlineData(100)]
    public void Performance_DifferentInputSizes_AllWork(int inputSize)
    {
        // Arrange
        var input = new byte[inputSize];
        var random = new Random(42);
        random.NextBytes(input);

        // Ensure no leading zeros (which would be lost in mathematical approach)
        if (input.Length > 0 && input[0] == 0)
        {
            input[0] = 1;
        }

        // Act
        var encoded = Base36.Encode(input);
        var bufferSize = Base36.GetByteCount(encoded);
        var decoded = new byte[bufferSize];
        var bytesWritten = Base36.Decode(encoded, decoded);

        // Assert
        Assert.Equal(inputSize, bytesWritten);
        Assert.True(input.AsSpan().SequenceEqual(decoded.AsSpan()[..bytesWritten]));
        Assert.NotEmpty(encoded);
    }

    [Fact]
    public void Decode_StringOverload_ReturnsCorrectString()
    {
        // Arrange
        const string original = "Hello World!";
        var bytes = Encoding.UTF8.GetBytes(original);
        var encoded = Base36.Encode(bytes);

        // Act
        var decoded = Base36.Decode(encoded);

        // Assert
        Assert.Equal(original, decoded);
    }

    [Fact]
    public void Decode_EmptyString_ReturnsEmptyString()
    {
        // Act
        var result = Base36.Decode("");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("1AS3ZUY1Q27UB", "1as3zuy1q27ub")]
    [InlineData("3YUD78MN", "3yud78mn")]
    [InlineData("1T", "1t")]
    public void Decode_MixedCase_ProducesSameResult(string uppercase, string lowercase)
    {
        // Arrange
        var bufferSize = Math.Max(Base36.GetByteCount(uppercase), Base36.GetByteCount(lowercase));
        Span<byte> buffer1 = stackalloc byte[bufferSize];
        Span<byte> buffer2 = stackalloc byte[bufferSize];

        // Act
        var bytes1 = Base36.Decode(uppercase, buffer1);
        var bytes2 = Base36.Decode(lowercase, buffer2);

        // Assert
        Assert.Equal(bytes1, bytes2);
        Assert.True(buffer1[..bytes1].SequenceEqual(buffer2[..bytes2]));
    }
}
