namespace HotChocolate.Subscriptions;

public class DefaultJsonMessageSerializerTests
{
    [Fact]
    public void DeserializeDefaultMessage()
    {
        // arrange
        var serializer = new DefaultJsonMessageSerializer();
        const string message = "{\"body\":\"abc\",\"kind\":0}";

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
        const string message = "{\"kind\":1}";

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
        const string message = "{\"kind\":1}";

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
        const string message = "{\"kind\":1}";

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
        const string message = "abc";

        // act
        var serializedMessage = serializer.Serialize(message);

        // assert
        Snapshot
            .Create()
            .Add(serializedMessage)
            .MatchInline("{\"body\":\"abc\",\"kind\":0}");
    }

    [Fact]
    public void Serialize_And_Deserialize_DefaultMessage_With_Interface_Property()
    {
        // arrange
        var serializer = new DefaultJsonMessageSerializer();
        var message = new ContainerMessage
        {
            Payload = new TextPayload
            {
                Text = "abc"
            }
        };

        // act
        var serialized = serializer.Serialize(message);
        var envelope = serializer.Deserialize<ContainerMessage>(serialized);

        // assert
        var payload = Assert.IsType<TextPayload>(envelope.Body!.Payload);
        Assert.Equal("abc", payload.Text);
    }

    public enum Foo
    {
        Bar
    }

    public interface IPayload;

    public sealed class TextPayload : IPayload
    {
        public string Text { get; set; } = default!;
    }

    public sealed class ContainerMessage
    {
        public IPayload Payload { get; set; } = default!;
    }
}
