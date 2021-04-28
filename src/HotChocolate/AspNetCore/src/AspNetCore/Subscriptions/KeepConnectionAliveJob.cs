using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions.Messages;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal sealed class KeepConnectionAliveJob
    {
        private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(5);
        private readonly ISocketConnection _connection;
        private readonly TimeSpan _timeout;

        public KeepConnectionAliveJob(ISocketConnection connection)
            : this(connection, _defaultTimeout)
        {
        }

        public KeepConnectionAliveJob(ISocketConnection connection, TimeSpan timeout)
        {
            _connection = connection;
            _timeout = timeout;
        }

        public void Begin(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(
                () => KeepConnectionAliveAsync(cancellationToken),
                cancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        private async Task KeepConnectionAliveAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                while (!_connection.Closed && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(_timeout, cancellationToken);

                    if (!_connection.Closed)
                    {
                        await _connection.SendAsync(
                            KeepConnectionAliveMessage.Default.Serialize(),
                            cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // the message processing was canceled.
            }
        }
    }
}
