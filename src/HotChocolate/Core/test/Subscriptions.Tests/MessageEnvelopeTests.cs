namespace HotChocolate.Subscriptions;

public class MessageEnvelopeTests
{
    [Fact]
    public void CreateDefaultMessage()
    {
        var message = new MessageEnvelope<string>("abc");
        Assert.Equal("abc", message.Body);
        Assert.Equal(MessageKind.Default, message.Kind);
    }

    [Fact]
    public void CreateCompletedMessage()
    {
        var message = new MessageEnvelope<string>(kind: MessageKind.Completed);
        Assert.Null(message.Body);
        Assert.Equal(MessageKind.Completed, message.Kind);
    }

    [Fact]
    public void CreateDefaultMessage_Body_Null()
    {
        Assert.Throws<ArgumentException>(
            () => new MessageEnvelope<string>(null));
    }

    [Fact]
    public void CreateCompletedMessage_Body_Not_Null()
    {
        var envelope = new MessageEnvelope<string>("abc", MessageKind.Completed);
        Assert.Null(envelope.Body);
    }

    [Fact]
    public void CreateCompletedMessage_Body_Not_Null_ValueType()
    {
        var envelope = new MessageEnvelope<int>(5, MessageKind.Completed);
        Assert.Equal(default, envelope.Body);
    }
}
