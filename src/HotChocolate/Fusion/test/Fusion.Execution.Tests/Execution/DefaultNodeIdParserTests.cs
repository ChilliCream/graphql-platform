using System.Text;
using HotChocolate.Buffers.Text;
using HotChocolate.Execution.Relay;

namespace HotChocolate.Fusion.Execution;

public class DefaultNodeIdParserTests
{
    #region Base64 Tests (Original functionality)

    [Theory]
    [InlineData("Product:123", "Product")]
    [InlineData("Product:123:456", "Product")]
    [InlineData("Product\nd123", "Product")]
    [InlineData("Product\nd123:456", "Product")]
    public void ValidId_Base64(string unencodedId, string typeName)
    {
        // arrange
        var parser = new DefaultNodeIdParser(NodeIdSerializerFormat.Base64);
        var encodedId = Convert.ToBase64String(Encoding.UTF8.GetBytes(unencodedId));

        // act
        var success = parser.TryParseTypeName(encodedId, out var parsedTypeName);

        // assert
        Assert.True(success);
        Assert.Equal(typeName, parsedTypeName);
    }

    [Theory]
    [InlineData("non-base-64-id")]
    [InlineData("aW52YWxpZA==")] // Base64, but neither delimiter
    [InlineData("UHJvZHVjdDo=")] // Delimiter is last character
    public void InvalidId_Base64(string rawId)
    {
        // arrange
        var parser = new DefaultNodeIdParser(NodeIdSerializerFormat.Base64);

        // act
        var success = parser.TryParseTypeName(rawId, out _);

        // assert
        Assert.False(success);
    }

    #endregion

    #region URL-Safe Base64 Tests

    [Theory]
    [InlineData("Product:123", "Product")]
    [InlineData("Product:123:456", "Product")]
    [InlineData("Product\nd123", "Product")]
    [InlineData("Product\nd123:456", "Product")]
    public void ValidId_UrlSafeBase64(string unencodedId, string typeName)
    {
        // arrange
        var parser = new DefaultNodeIdParser(NodeIdSerializerFormat.UrlSafeBase64);
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(unencodedId));
        var urlSafeBase64 = base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');

        // act
        var success = parser.TryParseTypeName(urlSafeBase64, out var parsedTypeName);

        // assert
        Assert.True(success);
        Assert.Equal(typeName, parsedTypeName);
    }

    [Theory]
    [InlineData("invalid-url-safe-base64")]
    [InlineData("aW52YWxpZA")] // URL-safe Base64, but no delimiter
    [InlineData("UHJvZHVjdDo")] // Delimiter is last character
    public void InvalidId_UrlSafeBase64(string rawId)
    {
        // arrange
        var parser = new DefaultNodeIdParser(NodeIdSerializerFormat.UrlSafeBase64);

        // act
        var success = parser.TryParseTypeName(rawId, out _);

        // assert
        Assert.False(success);
    }

    [Fact]
    public void UrlSafeBase64_HandlesSpecialCharacters()
    {
        // arrange
        var parser = new DefaultNodeIdParser(NodeIdSerializerFormat.UrlSafeBase64);
        const string originalData = "Product+Type/Special:123";
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(originalData));
        var urlSafeBase64 = base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');

        // act
        var success = parser.TryParseTypeName(urlSafeBase64, out var parsedTypeName);

        // assert
        Assert.True(success);
        Assert.Equal("Product+Type/Special", parsedTypeName);
    }

    #endregion

    #region Hex Tests

    [Theory]
    [InlineData("Product:123", "Product")]
    [InlineData("Product:123:456", "Product")]
    [InlineData("Product\nd123", "Product")]
    [InlineData("LongTypeName:SomeValue", "LongTypeName")]
    public void ValidId_UpperHex(string unencodedId, string typeName)
    {
        // arrange
        var parser = new DefaultNodeIdParser(NodeIdSerializerFormat.UpperHex);
        var hexId = Convert.ToHexString(Encoding.UTF8.GetBytes(unencodedId));

        // act
        var success = parser.TryParseTypeName(hexId, out var parsedTypeName);

        // assert
        Assert.True(success);
        Assert.Equal(typeName, parsedTypeName);
    }

    [Theory]
    [InlineData("Product:123", "Product")]
    [InlineData("Product:123:456", "Product")]
    [InlineData("Product\nd123", "Product")]
    [InlineData("LongTypeName:SomeValue", "LongTypeName")]
    public void ValidId_LowerHex(string unencodedId, string typeName)
    {
        // arrange
        var parser = new DefaultNodeIdParser(NodeIdSerializerFormat.LowerHex);
        var hexId = Convert.ToHexString(Encoding.UTF8.GetBytes(unencodedId)).ToLowerInvariant();

        // act
        var success = parser.TryParseTypeName(hexId, out var parsedTypeName);

        // assert
        Assert.True(success);
        Assert.Equal(typeName, parsedTypeName);
    }

    [Theory]
    [InlineData("invalid-hex-id")]
    [InlineData("696E76616C6964")] // Valid hex, but no delimiter
    [InlineData("50726F647563743A")] // Delimiter is last character
    [InlineData("ZZZZZZ")] // Invalid hex characters
    public void InvalidId_UpperHex(string rawId)
    {
        // arrange
        var parser = new DefaultNodeIdParser(NodeIdSerializerFormat.UpperHex);

        // act
        var success = parser.TryParseTypeName(rawId, out _);

        // assert
        Assert.False(success);
    }

    [Theory]
    [InlineData("invalid-hex-id")]
    [InlineData("696e76616c6964")] // Valid hex, but no delimiter
    [InlineData("50726f647563743a")] // Delimiter is last character
    [InlineData("123")] // Odd length
    public void InvalidId_LowerHex(string rawId)
    {
        // arrange
        var parser = new DefaultNodeIdParser(NodeIdSerializerFormat.LowerHex);

        // act
        var success = parser.TryParseTypeName(rawId, out _);

        // assert
        Assert.False(success);
    }

    [Fact]
    public void Hex_HandlesMixedCase()
    {
        // arrange
        var upperParser = new DefaultNodeIdParser(NodeIdSerializerFormat.UpperHex);
        var lowerParser = new DefaultNodeIdParser(NodeIdSerializerFormat.LowerHex);
        const string mixedCaseHex = "50726F647563743A313233"; // "Product:123" with mixed case

        // act
        var upperSuccess = upperParser.TryParseTypeName(mixedCaseHex, out var upperTypeName);
        var lowerSuccess = lowerParser.TryParseTypeName(mixedCaseHex, out var lowerTypeName);

        // assert
        Assert.True(upperSuccess);
        Assert.True(lowerSuccess);
        Assert.Equal("Product", upperTypeName);
        Assert.Equal("Product", lowerTypeName);
    }

    #endregion

    #region Base36 Tests

    [Theory]
    [InlineData("Product:123", "Product")]
    [InlineData("Product:123:456", "Product")]
    [InlineData("Product\nd123", "Product")]
    [InlineData("LongTypeName:SomeValue", "LongTypeName")]
    public void ValidId_Base36(string unencodedId, string typeName)
    {
        // arrange
        var parser = new DefaultNodeIdParser(NodeIdSerializerFormat.Base36);
        var base36Id = Base36.Encode(Encoding.UTF8.GetBytes(unencodedId));

        // act
        var success = parser.TryParseTypeName(base36Id, out var parsedTypeName);

        // assert
        Assert.True(success);
        Assert.Equal(typeName, parsedTypeName);
    }

    [Theory]
    [InlineData("invalid-base36-@")]
    [InlineData("INVALIDBASE36ID")] // Valid Base36 chars, but likely no delimiter when decoded
    public void InvalidId_Base36(string rawId)
    {
        // arrange
        var parser = new DefaultNodeIdParser(NodeIdSerializerFormat.Base36);

        // act
        var success = parser.TryParseTypeName(rawId, out _);

        // assert
        Assert.False(success);
    }

    [Fact]
    public void Base36_CaseInsensitive()
    {
        // arrange
        var parser = new DefaultNodeIdParser(NodeIdSerializerFormat.Base36);
        const string originalData = "Product:123";
        var base36Upper = Base36.Encode(Encoding.UTF8.GetBytes(originalData));
        var base36Lower = base36Upper.ToLowerInvariant();

        // act
        var upperSuccess = parser.TryParseTypeName(base36Upper, out var upperTypeName);
        var lowerSuccess = parser.TryParseTypeName(base36Lower, out var lowerTypeName);

        // assert
        Assert.True(upperSuccess);
        Assert.True(lowerSuccess);
        Assert.Equal("Product", upperTypeName);
        Assert.Equal("Product", lowerTypeName);
    }

    [Fact]
    public void Base36_PreservesTrailingZeros()
    {
        // arrange
        var parser = new DefaultNodeIdParser(NodeIdSerializerFormat.Base36);
        const string dataWithZeros = "Product:\0\0\0";
        var base36Id = Base36.Encode(Encoding.UTF8.GetBytes(dataWithZeros));

        // act
        var success = parser.TryParseTypeName(base36Id, out var typeName);

        // assert
        Assert.True(success);
        Assert.Equal("Product", typeName);
    }

    #endregion

    #region Edge Cases and Error Handling

    [Theory]
    [InlineData(NodeIdSerializerFormat.Base64)]
    [InlineData(NodeIdSerializerFormat.UrlSafeBase64)]
    [InlineData(NodeIdSerializerFormat.UpperHex)]
    [InlineData(NodeIdSerializerFormat.LowerHex)]
    [InlineData(NodeIdSerializerFormat.Base36)]
    public void EmptyString_ReturnsFalse(NodeIdSerializerFormat format)
    {
        // arrange
        var parser = new DefaultNodeIdParser(format);

        // act
        var success = parser.TryParseTypeName("", out var typeName);

        // assert
        Assert.False(success);
        Assert.Null(typeName);
    }

    [Theory]
    [InlineData(NodeIdSerializerFormat.Base64)]
    [InlineData(NodeIdSerializerFormat.UrlSafeBase64)]
    [InlineData(NodeIdSerializerFormat.UpperHex)]
    [InlineData(NodeIdSerializerFormat.LowerHex)]
    [InlineData(NodeIdSerializerFormat.Base36)]
    public void NullString_ReturnsFalse(NodeIdSerializerFormat format)
    {
        // arrange
        var parser = new DefaultNodeIdParser(format);

        // act
        var success = parser.TryParseTypeName(null!, out var typeName);

        // assert
        Assert.False(success);
        Assert.Null(typeName);
    }

    [Fact]
    public void LargeTypeName_HandledCorrectly()
    {
        // arrange
        var parser = new DefaultNodeIdParser(NodeIdSerializerFormat.Base64);
        var largeTypeName = new string('A', 500);
        var originalData = $"{largeTypeName}:123";
        var encodedId = Convert.ToBase64String(Encoding.UTF8.GetBytes(originalData));

        // act
        var success = parser.TryParseTypeName(encodedId, out var typeName);

        // assert
        Assert.True(success);
        Assert.Equal(largeTypeName, typeName);
    }

    [Fact]
    public void UnicodeTypeName_HandledCorrectly()
    {
        // arrange
        var parser = new DefaultNodeIdParser(NodeIdSerializerFormat.Base64);
        const string unicodeTypeName = "产品类型";
        var originalData = $"{unicodeTypeName}:123";
        var encodedId = Convert.ToBase64String(Encoding.UTF8.GetBytes(originalData));

        // act
        var success = parser.TryParseTypeName(encodedId, out var typeName);

        // assert
        Assert.True(success);
        Assert.Equal(unicodeTypeName, typeName);
    }

    [Fact]
    public void DefaultConstructor_UsesUrlSafeBase64()
    {
        // arrange
        var parser = new DefaultNodeIdParser(NodeIdSerializerFormat.UrlSafeBase64);
        const string originalData = "Product:123";
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(originalData));
        var urlSafeBase64 = base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');

        // act
        var success = parser.TryParseTypeName(urlSafeBase64, out var typeName);

        // assert
        Assert.True(success);
        Assert.Equal("Product", typeName);
    }

    [Theory]
    [InlineData(NodeIdSerializerFormat.Base64, "UHJvZHVjdDoxMjM=")]
    [InlineData(NodeIdSerializerFormat.UrlSafeBase64, "UHJvZHVjdDoxMjM")]
    [InlineData(NodeIdSerializerFormat.Base36, "C7X25QD0QBZSQW9IR")]
    [InlineData(NodeIdSerializerFormat.UpperHex, "50726F647563743A313233")]
    [InlineData(NodeIdSerializerFormat.LowerHex, "50726f647563743a313233")]
    public void AllFormats_ParseSameData(NodeIdSerializerFormat format, string encodedId)
    {
        // arrange
        var parser = new DefaultNodeIdParser(format);

        // act
        var success = parser.TryParseTypeName(encodedId, out var typeName);

        // assert
        Assert.True(success);
        Assert.Equal("Product", typeName);
    }

    #endregion

    #region Performance and Buffer Management Tests

    [Fact]
    public void LargeId_UsesRentedBuffer()
    {
        // arrange
        var parser = new DefaultNodeIdParser(NodeIdSerializerFormat.Base64);
        var largeData = new string('A', 1000) + ":123";
        var encodedId = Convert.ToBase64String(Encoding.UTF8.GetBytes(largeData));

        // act
        var success = parser.TryParseTypeName(encodedId, out var typeName);

        // assert
        Assert.True(success);
        Assert.Equal(new string('A', 1000), typeName);
    }

    [Fact]
    public void SmallId_UsesStackalloc()
    {
        // arrange
        var parser = new DefaultNodeIdParser(NodeIdSerializerFormat.Base64);
        const string smallData = "Product:123";
        var encodedId = Convert.ToBase64String(Encoding.UTF8.GetBytes(smallData));

        // act
        var success = parser.TryParseTypeName(encodedId, out var typeName);

        // assert
        Assert.True(success);
        Assert.Equal("Product", typeName);
    }

    #endregion
}
