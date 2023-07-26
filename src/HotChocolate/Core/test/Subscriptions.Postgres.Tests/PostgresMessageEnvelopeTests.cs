using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Subscriptions.Postgres;

public class PostgresMessageEnvelopeTests
{
    [Theory]
    [InlineData("test", "test")]
    [InlineData("sometopic", """ { ""test"": ""test"" } """)]
    public void Should_FormatAndParse(string topic, string payload)
    {
        // arrange
        var envelope = new PostgresMessageEnvelope(topic, payload);

        // act
        var formatted = envelope.Format();
        var parsingResult = PostgresMessageEnvelope
            .TryParse(formatted, out var parsedTopic, out var parsedPayload);

        // assert
        Assert.True(parsingResult);
        Assert.Equal(topic, parsedTopic);
        Assert.Equal(payload, parsedPayload);
    }

    [Theory]
    [InlineData("test", "test", "dGVzdA==:test")]
    [InlineData(
        "sometopic",
        """{ ""test"": ""test"" }""",
        """c29tZXRvcGlj:{ ""test"": ""test"" }""")]
    public void Should_FormatCorrectly(
        string topic,
        string payload,
        string formatted)
    {
        // arrange
        var envelope = new PostgresMessageEnvelope(topic, payload);

        // act
        var result = envelope.Format();

        // assert
        Assert.Equal(formatted, result[25..]);
    }

    [Fact]
    public void Format_Should_GenerateACorrectId()
    {
        var hitCharacters = new bool[26];
        var uniqueIds = new HashSet<string>();

        for (var i = 0; i < 10_000; i++)
        {
            // arrange
            var envelope = new PostgresMessageEnvelope("test", "test");

            // act
            var id = envelope.Format()[..24];

            // assert
            var bytes = Encoding.UTF8.GetBytes(id);
            Assert.Equal(24, bytes.Length);
            Assert.All(bytes, b => Assert.InRange(b, 97, 122));
            Assert.All(id, c => Assert.InRange(c, 'a', 'z'));

            for (var j = 0; j < id.Length; j++)
            {
                hitCharacters[id[j] - 'a'] = true;
            }

            uniqueIds.Add(id);
        }

        Assert.Equal(10_000, uniqueIds.Count);
        Assert.All(hitCharacters, Assert.True);
    }

    [Fact]
    public void Should_FormatAndParseWithBigPayload()
    {
        // arrange
        var topic = "test";
        var payload = new string('a', 100_000);
        var envelope = new PostgresMessageEnvelope(topic, payload);

        // act
        var formatted = envelope.Format();
        var parsingResult = PostgresMessageEnvelope
            .TryParse(formatted, out var parsedTopic, out var parsedPayload);

        // assert
        Assert.True(parsingResult);
        Assert.Equal(topic, parsedTopic);
        Assert.Equal(payload, parsedPayload);
    }

    [Fact]
    public void Should_FormatAndParseWithBigTopic()
    {
        // arrange
        var topic = new string('a', 100_000);
        var payload = "test";
        var envelope = new PostgresMessageEnvelope(topic, payload);

        // act
        var formatted = envelope.Format();
        var parsingResult = PostgresMessageEnvelope
            .TryParse(formatted, out var parsedTopic, out var parsedPayload);

        // assert
        Assert.True(parsingResult);
        Assert.Equal(topic, parsedTopic);
        Assert.Equal(payload, parsedPayload);
    }

    [Fact]
    public void Should_FormatAndParseWithBigTopicAndPayload()
    {
        // arrange
        var topic = new string('a', 100_000);
        var payload = new string('a', 100_000);
        var envelope = new PostgresMessageEnvelope(topic, payload);

        // act
        var formatted = envelope.Format();
        var parsingResult = PostgresMessageEnvelope
            .TryParse(formatted, out var parsedTopic, out var parsedPayload);

        // assert
        Assert.True(parsingResult);
        Assert.Equal(topic, parsedTopic);
        Assert.Equal(payload, parsedPayload);
    }
}
