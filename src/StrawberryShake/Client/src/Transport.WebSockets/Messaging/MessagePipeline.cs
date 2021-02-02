using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Transport;

namespace StrawberryShake.Http.Subscriptions
{
    /// <summary>
    /// Subscribes to a <see cref="ISocketClient"/> to receive data and pipes it through a
    /// <see cref="MessageProcessor"/> that executes the even handler passed into the
    /// <see cref="MessagePipeline"/>
    /// </summary>
    internal sealed class MessagePipeline : IAsyncDisposable
    {
        private readonly ISocketClient _client;
        private readonly ProcessAsync _processAsync;
        private CancellationTokenSource _cts = new();
        private int _state = State.Stopped;
        private Task _innerTask = Task.CompletedTask;

        public MessagePipeline(
            ISocketClient client,
            ProcessAsync processAsync)
        {
            _client = client;
            _processAsync = processAsync;
        }

        /// <summary>
        /// Start receiving message from the socket
        /// </summary>
        public void Start()
        {
            if (Interlocked.CompareExchange(ref _state, State.Running, State.Stopped) ==
                State.Stopped)
            {
                try
                {
                    var pipe = new Pipe();
                    _cts = new CancellationTokenSource();
                    _innerTask = Task.WhenAll(
                        MessageReceiver.Start(pipe.Writer, _client, _cts.Token),
                        MessageProcessor.Start(pipe.Reader, _processAsync, _cts.Token));
                }
                catch (TaskCanceledException) { }
                finally
                {
                    Interlocked.Exchange(ref _state, State.Running);
                }
            }
        }

        /// <summary>
        /// Stop receiving message from the socket
        /// </summary>
        public async Task Stop()
        {
            if (Interlocked.CompareExchange(ref _state, State.Stopped, State.Running) ==
                State.Running)
            {
                try
                {
                    _cts.Cancel();
                    await _innerTask.ConfigureAwait(false);
                }
                catch (TaskCanceledException) { }
                finally
                {
                    Interlocked.Exchange(ref _state, State.Stopped);
                }
            }
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            await Stop();

            _cts.Dispose();
        }

        /// <summary>
        /// The state of the receiver
        /// </summary>
        private static class State
        {
            public static readonly int Stopped = 0;
            public static readonly int Running = 1;
            public static readonly int Blocked = 2;
        }
    }
}
