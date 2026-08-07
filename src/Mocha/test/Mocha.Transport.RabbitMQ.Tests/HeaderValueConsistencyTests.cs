using System.Text.Json;
using System.Text.Json.Nodes;
using Mocha.Middlewares;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mocha.Transport.RabbitMQ.Tests;

/// <summary>
/// Asserts that the envelope serializer and the RabbitMQ dispatcher agree on what a header value
/// becomes.
/// </summary>
public class HeaderValueConsistencyTests
{
    public static TheoryData<string, object> TextValues()
        => new()
        {
            { "guid", Guid.Parse("12345678-1234-1234-1234-123456789012") },
            { "char", 'c' },
            { "uri", new Uri("https://example.com/a%20b") },
            { "timespan", TimeSpan.FromMinutes(90) },
            { "dateonly", new DateOnly(2026, 8, 3) },
            { "timeonly", new TimeOnly(10, 30, 15) },
            { "enum", DayOfWeek.Wednesday }
        };

    [Theory]
    [MemberData(nameof(TextValues))]
    public void BothEncoders_Should_WriteTheSameText_When_ValueTravelsAsText(string key, object value)
    {
        // arrange
        var headers = new Headers([new HeaderValue { Key = key, Value = value }]);
        var envelope = new MessageEnvelope { Headers = headers };

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);
        var table = envelope.BuildHeaders();

        // assert
        Assert.Equal(
            JsonSerializer.Serialize(new Dictionary<string, object?> { [key] = table[key] }),
            json);
    }

    [Theory]
    [MemberData(nameof(ShapedValues))]
    public void BothEncoders_Should_ProduceTheSameReading_When_ValueIsAContainer(string key, object value)
    {
        // arrange
        var headers = new Headers([new HeaderValue { Key = key, Value = value }]);
        var envelope = new MessageEnvelope { Headers = headers };

        // act
        var throughJson = JsonSerializer.Deserialize<IHeaders>(
            JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options),
            HeadersJsonConverter.Options);
        var throughTable = RabbitMQMessageEnvelopeParser.Instance
            .Parse(CreateDelivery(envelope.BuildHeaders()));

        // assert
        throughJson!.TryGetValue(key, out var fromJson);
        throughTable.Headers!.TryGetValue(key, out var fromTable);
        Assert.Equal(fromJson, fromTable);
    }

    public static TheoryData<string, object> ShapedValues()
        => new()
        {
            { "dictionary", new Dictionary<string, object?> { ["k"] = "v" } },
            { "sequence", new object?[] { 1, "a" } },
            { "json-object", new JsonObject { ["k"] = 1 } },
            { "json-array", new JsonArray(1, 2) },
            { "json-text", JsonValue.Create("abc")! },
            // cloned so that the element outlives the document it was parsed from
            { "json-element", JsonDocument.Parse("""{"k":1}""").RootElement.Clone() },
            { "json-document", JsonDocument.Parse("""[1,2]""") },
            { "nested-headers", new Headers([new HeaderValue { Key = "inner", Value = "v" }]) },
            { "null-inside", new Dictionary<string, object?> { ["k"] = null } }
        };

    [Fact]
    public void BothEncoders_Should_KeepTheHeader_When_ItsValueIsNull()
    {
        // arrange
        var headers = new Headers([new HeaderValue { Key = "x-null", Value = null }]);
        var envelope = new MessageEnvelope { Headers = headers };

        // act
        var throughJson = JsonSerializer.Deserialize<IHeaders>(
            JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options),
            HeadersJsonConverter.Options);
        var throughTable = RabbitMQMessageEnvelopeParser.Instance
            .Parse(CreateDelivery(envelope.BuildHeaders()));

        // assert
        Assert.True(throughJson!.ContainsKey("x-null"));
        Assert.True(throughTable.Headers!.ContainsKey("x-null"));
    }

    private static BasicDeliverEventArgs CreateDelivery(IDictionary<string, object?> headers)
    {
        var properties = new BasicProperties { Headers = headers };

        return new BasicDeliverEventArgs(
            consumerTag: "tag",
            deliveryTag: 1,
            redelivered: false,
            exchange: "exchange",
            routingKey: "key",
            properties: properties,
            body: ReadOnlyMemory<byte>.Empty);
    }
}
