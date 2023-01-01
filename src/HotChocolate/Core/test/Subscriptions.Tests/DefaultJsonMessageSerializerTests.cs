using CookieCrumble;

namespace HotChocolate.Subscriptions;

public class DefaultJsonMessageSerializerTests
{
    [Fact]
    public void SerializeCompleteMessage()
    {
        // arrange
        var serializer = new DefaultJsonMessageSerializer();
        var message = new MessageEnvelope<object>(kind: MessageKind.Completed);

        // act
        var serializedMessage = serializer.Serialize(message);

        // assert
        Snapshot
            .Create()
            .Add(serializedMessage)
            .MatchInline("{\"kind\":1}");
    }

     [Fact]
    public void CompleteMessagePropIsEqualToSerializationResult()
    {
        // arrange
        var serializer = new DefaultJsonMessageSerializer();
        var message = new MessageEnvelope<object>(kind: MessageKind.Completed);

        // act
        var serializedMessage = serializer.Serialize(message);

        // assert
        Assert.Equal(serializer.CompleteMessage, serializedMessage);
    }

    [Fact]
    public void SerializeUnsubscribedMessage()
    {
        // arrange
        var serializer = new DefaultJsonMessageSerializer();
        var message = new MessageEnvelope<object>(kind: MessageKind.Unsubscribed);

        // act
        var serializedMessage = serializer.Serialize(message);

        // assert
        Snapshot
            .Create()
            .Add(serializedMessage)
            .MatchInline("{\"kind\":2}");
    }

        [Fact]
    public void SerializeDefaultMessage()
    {
        // arrange
        var serializer = new DefaultJsonMessageSerializer();
        var message = new MessageEnvelope<string>(body: "abc", kind: MessageKind.Default);

        // act
        var serializedMessage = serializer.Serialize(message);

        // assert
        Snapshot
            .Create()
            .Add(serializedMessage)
            .MatchInline("{\"body\":\"abc\",\"kind\":0}");
    }
}
