using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace Mocha.Tests;

public class HeadersJsonConverterTests
{
    [Fact]
    public void Write_Should_EmitStringValue_When_HeaderIsGuid()
    {
        // arrange
        var headers = new Headers();
        var value = Guid.Parse("12345678-1234-1234-1234-123456789012");
        headers.Set("id", value);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        Assert.Equal("""{"id":"12345678-1234-1234-1234-123456789012"}""", json);
    }

    [Fact]
    public void Write_Should_EmitIsoString_When_HeaderIsTimeSpan()
    {
        // arrange
        var headers = new Headers();
        var value = new TimeSpan(1, 2, 3, 4);
        headers.Set("duration", value);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        var expected = value.ToString("c", CultureInfo.InvariantCulture);
        Assert.Equal($$"""{"duration":"{{expected}}"}""", json);
    }

    [Fact]
    public void Write_Should_EmitStringValue_When_HeaderIsUri()
    {
        // arrange
        var headers = new Headers();
        var value = new Uri("https://example.com/path");
        headers.Set("url", value);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        Assert.Equal("""{"url":"https://example.com/path"}""", json);
    }

    [Fact]
    public void Write_Should_EmitIsoDate_When_HeaderIsDateOnly()
    {
        // arrange
        var headers = new Headers();
        var value = new DateOnly(2024, 1, 15);
        headers.Set("date", value);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        var expected = value.ToString("O", CultureInfo.InvariantCulture);
        Assert.Equal($$"""{"date":"{{expected}}"}""", json);
    }

    [Fact]
    public void Write_Should_EmitIsoTime_When_HeaderIsTimeOnly()
    {
        // arrange
        var headers = new Headers();
        var value = new TimeOnly(10, 30, 45);
        headers.Set("time", value);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        var expected = value.ToString("O", CultureInfo.InvariantCulture);
        Assert.Equal($$"""{"time":"{{expected}}"}""", json);
    }

    [Fact]
    public void Write_Should_EmitEnumName_When_HeaderIsEnum()
    {
        // arrange
        var headers = new Headers();
        headers.Set("priority", TestPriority.High);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        Assert.Equal("""{"priority":"High"}""", json);
    }

    [Fact]
    public void Write_Should_EmitNumber_When_HeaderIsShort()
    {
        // arrange
        var headers = new Headers();
        headers.Set("value", (short)123);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        Assert.Equal("""{"value":123}""", json);
    }

    [Fact]
    public void Write_Should_EmitNumber_When_HeaderIsUshort()
    {
        // arrange
        var headers = new Headers();
        headers.Set("value", (ushort)456);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        Assert.Equal("""{"value":456}""", json);
    }

    [Fact]
    public void Write_Should_EmitNumber_When_HeaderIsByte()
    {
        // arrange
        var headers = new Headers();
        headers.Set("value", (byte)7);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        Assert.Equal("""{"value":7}""", json);
    }

    [Fact]
    public void Write_Should_EmitNumber_When_HeaderIsSbyte()
    {
        // arrange
        var headers = new Headers();
        headers.Set("value", (sbyte)-8);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        Assert.Equal("""{"value":-8}""", json);
    }

    [Fact]
    public void Write_Should_EmitNumber_When_HeaderIsUint()
    {
        // arrange
        var headers = new Headers();
        headers.Set("value", 4000000000u);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        Assert.Equal("""{"value":4000000000}""", json);
    }

    [Fact]
    public void Write_Should_EmitNumber_When_HeaderIsUlong()
    {
        // arrange
        var headers = new Headers();
        headers.Set("value", 18000000000000000000ul);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        Assert.Equal("""{"value":18000000000000000000}""", json);
    }

    [Fact]
    public void Write_Should_EmitStringValue_When_HeaderIsChar()
    {
        // arrange
        var headers = new Headers();
        headers.Set("letter", 'A');

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        Assert.Equal("""{"letter":"A"}""", json);
    }

    [Fact]
    public void Write_Should_KeepEscapes_When_HeaderIsUri()
    {
        // arrange
        var headers = new Headers();
        headers.Set("callback", new Uri("https://example.com/a%20b"));

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        Assert.Equal("""{"callback":"https://example.com/a%20b"}""", json);
    }

    [Fact]
    public void Write_Should_EmitBase64String_When_HeaderIsByteMemory()
    {
        // arrange
        var headers = new Headers();
        headers.Set("readonly", new ReadOnlyMemory<byte>("COM1"u8.ToArray()));
        headers.Set("writable", new Memory<byte>("COM1"u8.ToArray()));

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        Assert.Equal("""{"readonly":"Q09NMQ==","writable":"Q09NMQ=="}""", json);
    }

    [Fact]
    public void Write_Should_EmitBase64String_When_HeaderIsByteArray()
    {
        // arrange
        var headers = new Headers();
        headers.Set("signature", "signature-123"u8.ToArray());

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        Assert.Equal("""{"signature":"c2lnbmF0dXJlLTEyMw=="}""", json);
    }

    [Fact]
    public void Write_Should_EmitBase64Strings_When_ByteArraysAreNestedInHeader()
    {
        // arrange
        // the shape a broker-generated x-death header has: an array of tables
        var headers = new Headers();
        headers.Set(
            "x-death",
            new List<object?>
            {
                new Dictionary<string, object?>
                {
                    ["reason"] = "rejected"u8.ToArray(),
                    ["count"] = 1L
                }
            });

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        Assert.Equal("""{"x-death":[{"reason":"cmVqZWN0ZWQ=","count":1}]}""", json);
    }

    [Fact]
    public void Write_Should_ThrowJsonExceptionNamingTheHeader_When_ValueTypeIsUnsupported()
    {
        // arrange
        var headers = new Headers();
        headers.Set("handle", nint.Zero);

        // act
        var exception = Record.Exception(
            () => JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options));

        // assert
        Assert.Equal(
            "The header 'handle' holds a value of type 'System.IntPtr' that cannot be written as "
                + "JSON. Set the header to a supported value type, or register JSON type metadata for it.",
            Assert.IsType<JsonException>(exception).Message);
    }

    [Fact]
    public void Write_Should_ThrowJsonExceptionNamingTheHeader_When_HeaderIsCustomType()
    {
        // arrange
        // a custom type has no JSON metadata, and none is inferred through reflection
        var headers = new Headers();
        headers.Set("custom", new CustomHeaderDto { Name = "test" });

        // act
        var exception = Record.Exception(
            () => JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options));

        // assert
        Assert.Equal(
            "The header 'custom' holds a value of type 'Mocha.Tests.HeadersJsonConverterTests+"
                + "CustomHeaderDto' that cannot be written as JSON. Set the header to a supported value "
                + "type, or register JSON type metadata for it.",
            Assert.IsType<JsonException>(exception).Message);
    }

    [Fact]
    public void Write_Should_EmitArray_When_HeaderIsValueTypeSequence()
    {
        // arrange
        // a sequence of value types has no covariant conversion to IEnumerable<object?>
        var headers = new Headers();
        headers.Set("attempts", new[] { 1, 2, 3 });
        headers.Set("offsets", new List<long> { 10L });

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        Assert.Equal("""{"attempts":[1,2,3],"offsets":[10]}""", json);
    }

    [Fact]
    public void Write_Should_EmitObject_When_HeaderIsValueTypeDictionary()
    {
        // arrange
        var headers = new Headers();
        headers.Set("tags", new Dictionary<string, string> { ["region"] = "eu" });
        headers.Set("counts", new SortedDictionary<string, int> { ["retries"] = 2 });

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        Assert.Equal("""{"tags":{"region":"eu"},"counts":{"retries":2}}""", json);
    }

    [Fact]
    public void Write_Should_ThrowJsonExceptionNamingTheHeader_When_DictionaryKeyIsNotString()
    {
        // arrange
        var headers = new Headers();
        headers.Set("weights", new Dictionary<int, string> { [1] = "high" });

        // act
        var exception = Record.Exception(
            () => JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options));

        // assert
        Assert.Equal(
            "The header 'weights' holds a dictionary keyed by 'System.Int32' that cannot be written "
                + "as JSON. Use string keys.",
            Assert.IsType<JsonException>(exception).Message);
    }

    [Fact]
    public void Write_Should_EmitJsonVerbatim_When_HeaderIsJsonNode()
    {
        // arrange
        var headers = new Headers();
        headers.Set("trace", new JsonObject { ["id"] = "abc", ["depth"] = 3 });
        headers.Set("hops", new JsonArray("a", "b"));

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        Assert.Equal("""{"trace":{"id":"abc","depth":3},"hops":["a","b"]}""", json);
    }

    [Fact]
    public void Write_Should_EmitBase64String_When_HeaderIsByteSegment()
    {
        // arrange
        // a byte payload is base64 regardless of the container it arrives in
        var headers = new Headers();
        headers.Set("signature", new ArraySegment<byte>("signature-123"u8.ToArray()));

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        Assert.Equal("""{"signature":"c2lnbmF0dXJlLTEyMw=="}""", json);
    }

    [Fact]
    public void Read_Should_KeepText_When_ValueLooksLikeATimestamp()
    {
        // arrange
        const string json = """{"fault-timestamp":"2026-08-03T10:00:00.0000000+02:00"}""";

        // act
        var headers = JsonSerializer.Deserialize<IHeaders>(json, HeadersJsonConverter.Options);

        // assert
        headers!.TryGetValue("fault-timestamp", out var value);
        Assert.Equal("2026-08-03T10:00:00.0000000+02:00", Assert.IsType<string>(value));
    }

    [Fact]
    public void Read_Should_ReturnUnsignedLong_When_NumberExceedsSignedRange()
    {
        // arrange
        const string json = """{"offset":18446744073709551615}""";

        // act
        var headers = JsonSerializer.Deserialize<IHeaders>(json, HeadersJsonConverter.Options);

        // assert
        headers!.TryGetValue("offset", out var value);
        Assert.Equal(ulong.MaxValue, Assert.IsType<ulong>(value));
    }

    [Fact]
    public void ReadWrite_Should_PreserveJson_When_ValuesAreRoundTripped()
    {
        // arrange
        // every canonical read type, including the two that used to be rewritten
        const string json = """
            {"text":"2026-08-03T10:00:00.0000000Z","offset":18446744073709551615,"count":1,"ticks":9000000000,"ratio":2.5,"flag":true,"empty":null,"table":{"k":"v"},"list":[1,"a"]}
            """;

        // act
        var first = JsonSerializer.Serialize(
            JsonSerializer.Deserialize<IHeaders>(json, HeadersJsonConverter.Options),
            HeadersJsonConverter.Options);
        var second = JsonSerializer.Serialize(
            JsonSerializer.Deserialize<IHeaders>(first, HeadersJsonConverter.Options),
            HeadersJsonConverter.Options);

        // assert
        Assert.Equal(json, first);
        Assert.Equal(first, second);
    }

    [Fact]
    public void Options_Should_RoundTrip_When_TheConcreteTypeIsUsed()
    {
        // arrange
        var headers = new Headers();
        headers.Set("trace-id", "abc-123");

        // act
        var json = JsonSerializer.Serialize(headers, HeadersJsonConverter.Options);
        var result = JsonSerializer.Deserialize<Headers>(json, HeadersJsonConverter.Options);

        // assert
        result!.TryGetValue("trace-id", out var value);
        Assert.Equal("""{"trace-id":"abc-123"}""", json);
        Assert.Equal("abc-123", value);
    }

    [Fact]
    public void Options_Should_BeSealed_When_FirstObserved()
    {
        // arrange
        var options = HeadersJsonConverter.Options;

        // act
        var exception = Record.Exception(() => options.TypeInfoResolver = new DefaultJsonTypeInfoResolver());

        // assert
        Assert.True(options.IsReadOnly);
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void Options_Should_ResolveOnlyHeaders_When_MetadataIsRequested()
    {
        // arrange
        // the resolver covers headers only, so nothing pulls in the reflection-based resolver
        var resolver = HeadersJsonConverter.Options.TypeInfoResolver!;

        // act
        var headers = resolver.GetTypeInfo(typeof(IHeaders), HeadersJsonConverter.Options);
        var unrelated = resolver.GetTypeInfo(typeof(CustomHeaderDto), HeadersJsonConverter.Options);

        // assert
        Assert.Equal(typeof(IHeaders), headers!.Type);
        Assert.Null(unrelated);
    }

    private enum TestPriority
    {
        Low,
        Normal,
        High
    }

    private sealed class CustomHeaderDto
    {
        public string Name { get; init; } = "";
    }
}
