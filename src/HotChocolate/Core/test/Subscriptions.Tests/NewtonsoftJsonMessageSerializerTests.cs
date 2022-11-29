using CookieCrumble;

namespace HotChocolate.Subscriptions;

public class NewtonsoftJsonMessageSerializerTests
{
    [Fact]
    public void SerializeCompleteMessage()
    {
        // arrange
        var serializer = new NewtonsoftJsonMessageSerializer();
        var message = new MessageEnvelope<object>(kind: MessageKind.Completed);

        // act
        var serializedMessage = serializer.Serialize(message);

        // assert
        Snapshot
            .Create()
            .Add(serializedMessage)
            .MatchInline(
                @"{""$type"":""HotChocolate.Subscriptions.MessageEnvelope`1[[System.Object, " +
                @"System.Private.CoreLib]], HotChocolate.Subscriptions""," +
                @"""Body"":null,""Kind"":1}");
    }

    [Fact]
    public void DeserializeCacheCompletedMessage()
    {
        // arrange
        var serializer = new NewtonsoftJsonMessageSerializer();

        // act
        var message = serializer.Deserialize<MessageEnvelope<object>>(
            serializer.CompleteMessage);

        // assert
        Assert.Equal(MessageKind.Completed, message.Kind);
        Assert.Null(message.Body);
    }

    [Fact]
    public void SerializeUnsubscribedMessage()
    {
        // arrange
        var serializer = new NewtonsoftJsonMessageSerializer();
        var message = new MessageEnvelope<object>(kind: MessageKind.Unsubscribed);

        // act
        var serializedMessage = serializer.Serialize(message);

        // assert
        Snapshot
            .Create()
            .Add(serializedMessage)
            .MatchInline(
                "{\"$type\":\"HotChocolate.Subscriptions.MessageEnvelope`1[[System.Object, " +
                "System.Private.CoreLib]], HotChocolate.Subscriptions\",\"Body\":null,\"Kind\":2}");
    }

    [Fact]
    public void SerializeDefaultMessage()
    {
        // arrange
        var serializer = new NewtonsoftJsonMessageSerializer();
        var message = new MessageEnvelope<string>(body: "abc", kind: MessageKind.Default);

        // act
        var serializedMessage = serializer.Serialize(message);

        // assert
        Snapshot
            .Create()
            .Add(serializedMessage)
            .MatchInline(
                "{\"$type\":\"HotChocolate.Subscriptions.MessageEnvelope`1[[System.String, " +
                "System.Private.CoreLib]], HotChocolate.Subscriptions\"," +
                "\"Body\":\"abc\",\"Kind\":0}");
    }
}
