using System.Text;

namespace HotChocolate.Transport.Http;

public class SseEventParserTests
{
    [Fact]
    public void ParsesNextEvent_WithSingleDataLine()
    {
        var sse = "event: next\ndata: {\"foo\":\"bar\"}\n\n"u8.ToArray();
        var result = SseEventParser.Parse(sse);

        Assert.Equal(SseEventType.Next, result.Type);
        Assert.NotNull(result.Data);
        Assert.Equal("{\"foo\":\"bar\"}", Encoding.UTF8.GetString(result.Data!.WrittenSpan));
    }

    [Fact]
    public void ParsesNextEvent_WithMultipleDataLines()
    {
        var sse = "event: next\ndata: {\ndata:   \"foo\": \"bar\"\ndata: }\n\n"u8.ToArray();
        var result = SseEventParser.Parse(sse);

        Assert.Equal(SseEventType.Next, result.Type);
        Assert.NotNull(result.Data);
        Assert.Equal("{\n\"foo\": \"bar\"\n}", Encoding.UTF8.GetString(result.Data!.WrittenSpan));
    }

    [Fact]
    public void ParsesCompleteEvent()
    {
        var sse = "event: complete\n\n"u8.ToArray();
        var result = SseEventParser.Parse(sse);

        Assert.Equal(SseEventType.Complete, result.Type);
        Assert.Null(result.Data);
    }

    [Fact]
    public void ReturnsUnknownEventType_ForInvalidEvent()
    {
        var sse = "event: invalid\n\n"u8.ToArray();
        var result = SseEventParser.Parse(sse);

        Assert.Equal(SseEventType.Unknown, result.Type);
        Assert.Null(result.Data);
    }

    [Fact]
    public void ParsesDataField_WithEmptyLines()
    {
        var sse = "event: next\ndata:\ndata: {\"a\":1}\ndata:\n\n"u8.ToArray();
        var result = SseEventParser.Parse(sse);

        Assert.Equal(SseEventType.Next, result.Type);
        Assert.NotNull(result.Data);
        Assert.Equal("{\"a\":1}\n", Encoding.UTF8.GetString(result.Data!.WrittenSpan));
    }

    [Fact]
    public void ParsesDataField_WithEmptyLines_RN()
    {
        var sse = "event: next\r\ndata:\r\ndata: {\"a\":1}\r\ndata:\r\n\r\n"u8.ToArray();
        var result = SseEventParser.Parse(sse);

        Assert.Equal(SseEventType.Next, result.Type);
        Assert.NotNull(result.Data);
        Assert.Equal("{\"a\":1}\n", Encoding.UTF8.GetString(result.Data!.WrittenSpan));
    }

    [Fact]
    public void Throws_OnMalformedDataStart()
    {
        var sse = "event: next\nfoo: bar\n\n"u8.ToArray();

        Assert.Throws<GraphQLHttpStreamException>(() => SseEventParser.Parse(sse));
    }
}
