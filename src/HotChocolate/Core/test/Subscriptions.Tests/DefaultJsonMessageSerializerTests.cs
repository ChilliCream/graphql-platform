namespace HotChocolate.Subscriptions;

public class DefaultJsonMessageSerializerTests
{
    [Fact]
    public void DeserializeDefaultMessage()
    {
        // arrange
        var serializer = new DefaultJsonMessageSerializer();
        var message = "{\"body\":\"abc\",\"kind\":0}";

        // act
        var messageEnvelope = serializer.Deserialize<string>(message);

        // assert
        Assert.Equal(MessageKind.Default, messageEnvelope.Kind);
        Assert.Equal("abc", messageEnvelope.Body);
    }

    [Fact]
    public void DeserializeCompleteMessage()
    {
        // arrange
        var serializer = new DefaultJsonMessageSerializer();
        var message = "{\"kind\":1}";

        // act
        var messageEnvelope = serializer.Deserialize<string>(message);

        // assert
        Assert.Equal(MessageKind.Completed, messageEnvelope.Kind);
    }

    [Fact]
    public void DeserializeCompleteMessage_With_Enum_Body()
    {
        // arrange
        var serializer = new DefaultJsonMessageSerializer();
        var message = "{\"kind\":1}";

        // act
        var messageEnvelope = serializer.Deserialize<Foo>(message);

        // assert
        Assert.Equal(MessageKind.Completed, messageEnvelope.Kind);
    }

    [Fact]
    public void DeserializeCompleteMessage_With_Int_Body()
    {
        // arrange
        var serializer = new DefaultJsonMessageSerializer();
        var message = "{\"kind\":1}";

        // act
        var messageEnvelope = serializer.Deserialize<int>(message);

        // assert
        Assert.Equal(MessageKind.Completed, messageEnvelope.Kind);
    }

    [Fact]
    public void SerializeDefaultMessage()
    {
        // arrange
        var serializer = new DefaultJsonMessageSerializer();
        var message = "abc";

        // act
        var serializedMessage = serializer.Serialize(message);

        // assert
        Snapshot
            .Create()
            .Add(serializedMessage)
            .MatchInline("{\"body\":\"abc\",\"kind\":0}");
    }

    public enum Foo
    {
        Bar,
    }
}
