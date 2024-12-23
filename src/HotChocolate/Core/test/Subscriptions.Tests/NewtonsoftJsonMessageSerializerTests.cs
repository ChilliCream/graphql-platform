namespace HotChocolate.Subscriptions;

public class NewtonsoftJsonMessageSerializerTests
{
    [Fact]
    public void DeserializeCacheCompletedMessage()
    {
        // arrange
        var serializer = new NewtonsoftJsonMessageSerializer();

        // act
        var message = serializer.Deserialize<object>(
            serializer.CompleteMessage);

        // assert
        Assert.Equal(MessageKind.Completed, message.Kind);
        Assert.Null(message.Body);
    }

    [Fact]
    public void SerializeDefaultMessage()
    {
        // arrange
        var serializer = new NewtonsoftJsonMessageSerializer();
        var message = "abc";

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
