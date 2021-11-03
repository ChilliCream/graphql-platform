using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Transport.Sockets;

namespace HotChocolate.Transport.Sockets.Protocols.GraphQLOverWebSocket;

internal class MessageSender : IMessageSender
{
    public ValueTask SendAsync(ISocketSession session, IMessage message, CancellationToken cancellationToken)
    {
        return default;
    }
}

internal class MessageReceiver : IMessageReceiver
{
    public async ValueTask OnReceiveAsync(
        ISocketSession session,
        IMessage message,
        CancellationToken cancellationToken)
    {
        switch (message.Type)
        {
            case MessageTypes.Initialize:
                if (session.IsInitialized)
                {
                    throw new Exception("");
                }

                // TODO : socket interceptor.
                var response = ConnectionAckMessage.Default;
                await session.SendAsync(response, cancellationToken).ConfigureAwait(false);
                break;

            case MessageTypes.Pong:
                return;

            case MessageTypes.Subscribe:
                await OnSubscribeAsync(session, (SubscribeMessage)message, cancellationToken).ConfigureAwait(false);

                break;
            case MessageTypes.Complete:
                session.TryCompleteOperation(((CompleteMessage)message).Id);
                break;

            default:
                // close socket unknown message
                break;
        }
    }

    private async ValueTask OnSubscribeAsync(
        ISocketSession session,
        SubscribeMessage message,
        CancellationToken cancellationToken)
    {
        if (message.Payload.Query is null ||
            (message.Payload.Extensions?.ContainsKey("id") ?? false))
        {
            var error = new ErrorMessage(message.Id, new Error("Validation ERROR"));
            await session.SendAsync(error, cancellationToken).ConfigureAwait(false);
            return;
        }

        var builder = new QueryRequestBuilder();

        if (message.Payload.Query is not null)
        {
            builder.SetQuery(message.Payload.Query);
        }

        if (message.Payload.Extensions?.ContainsKey("id") ?? false)
        {
            builder.SetQueryId((string)message.Payload.Extensions["id"]!);
        }

        if (message.Payload.OperationName is not null)
        {
            builder.SetOperation(message.Payload.OperationName);
        }

        if (message.Payload.Extensions is not null)
        {
            builder.SetExtensions(message.Payload.Extensions);
        }

        if (message.Payload.Variables is not null)
        {
            builder.SetVariableValues(message.Payload.Variables);
        }

        IExecutionResult result = await session.Executor.ExecuteAsync(builder.Create(), cancellationToken).ConfigureAwait(false);

        if (result is IResponseStream stream)
        {
            StartProcessingStream(session.RegisterOperation(stream));
        }
        else if (result is IQueryResult single)
        {
            await session.SendAsync(new NextMessage(message.Id, single), cancellationToken);
            await session.SendAsync(new CompleteMessage(message.Id), cancellationToken);
        }

        throw new Exception("Something is wrong.");
    }

    private void StartProcessingStream(IStreamOperation operation)
    {
        Task.Run(async () => await ProcessStreamAsync(operation));
    }

    private async Task ProcessStreamAsync(IStreamOperation operation)
    {
        try
        {
            ISocketSession session = operation.Session;
            CancellationToken ct = operation.RequestAborted;

            await foreach (IQueryResult result in operation.Stream.ReadResultsAsync().WithCancellation(ct).ConfigureAwait(false))
            {
                await session.SendAsync(new NextMessage(operation.Id, result), ct).ConfigureAwait(false);
            }

            await session.SendAsync(new CompleteMessage(operation.Id), ct).ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            operation.Dispose();
        }
    }
}
