using System.IO.Pipelines;

namespace HotChocolate.AspNetCore.Subscriptions;

internal sealed class MessageReceiver
{
    private readonly ISocketConnection _connection;
    private readonly PipeWriter _writer;

    public MessageReceiver(ISocketConnection connection, PipeWriter writer)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    public async Task ReceiveAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!_connection.IsClosed && !cancellationToken.IsCancellationRequested)
            {
                await _connection.ReceiveAsync(_writer, cancellationToken);
                await _writer.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // if the connection was cancelled we will swallow the exception and move on.
        }
        finally
        {
            // writer should be always completed
            await _writer.CompleteAsync();
        }
    }
}
