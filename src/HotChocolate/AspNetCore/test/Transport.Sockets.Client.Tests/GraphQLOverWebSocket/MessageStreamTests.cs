using System.Buffers;
using HotChocolate.Transport.Sockets.Client.Protocols;
using HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket.Messages;

namespace HotChocolate.Transport.Sockets.GraphQLOverWebSocket;

public class MessageStreamTests
{
    [Fact]
    public void OnNext_Should_DisposeNextPayload_When_NoObserverMatches()
    {
        // arrange
        var stream = new MessageStream();
        var message = NextMessage.From(
            new ReadOnlySequence<byte>(
                """{"type":"next","id":"unmatched","payload":{"data":{"value":1}}}"""u8.ToArray()));

        // act
        stream.OnNext(message);

        // assert
        Assert.Throws<ObjectDisposedException>(() => message.Payload.Data.GetRawText());
    }

    [Fact]
    public void OnNext_Should_DisposeErrorPayload_When_NoObserverMatches()
    {
        // arrange
        var stream = new MessageStream();
        var message = ErrorMessage.From(
            new ReadOnlySequence<byte>(
                """{"type":"error","id":"unmatched","payload":[{"message":"failure"}]}"""u8.ToArray()));

        // act
        stream.OnNext(message);

        // assert
        Assert.Throws<ObjectDisposedException>(() => message.Payload.Errors.GetRawText());
    }
}
