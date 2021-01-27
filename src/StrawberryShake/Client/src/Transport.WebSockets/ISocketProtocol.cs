using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.Transport
{
    public delegate ValueTask OnReceiveAsync(
        string messageId,
        JsonDocument document,
        CancellationToken cancellationToken);

    public interface ISocketProtocol : IAsyncDisposable
    {
        string ProtocolName { get; }

        event EventHandler Disposed;

        Task StartOperationAsync(
            string operationId,
            OperationRequest request,
            CancellationToken cancellationToken);

        Task StopOperationAsync(
            string operationId,
            CancellationToken cancellationToken);

        Task InitializeAsync(ISocketClient socketClient, CancellationToken cancellationToken);

        Task TerminateAsync(CancellationToken cancellationToken);

        void Subscribe(OnReceiveAsync listener);

        void Unsubscribe(OnReceiveAsync listener);

        ValueTask Notify(
            string messageId,
            JsonDocument document,
            CancellationToken cancellationToken);
    }
}
