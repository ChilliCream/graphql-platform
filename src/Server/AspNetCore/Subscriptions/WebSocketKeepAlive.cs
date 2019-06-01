#if !ASPNETCLASSIC
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal sealed class WebSocketKeepAlive
    {
        private readonly IWebSocketContext _context;
        private readonly TimeSpan _timeout;
        private readonly CancellationTokenSource _cts;

        public WebSocketKeepAlive(
            IWebSocketContext context,
            TimeSpan timeout,
            CancellationTokenSource cts)
        {
            _context = context;
            _timeout = timeout;
            _cts = cts;
        }

        public void Start()
        {
            Task.Factory.StartNew(
                KeepConnectionAliveAsync,
                _cts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        private async Task KeepConnectionAliveAsync()
        {
            while (!_context.Closed || !_cts.IsCancellationRequested)
            {
                await Task.Delay(_timeout, _cts.Token)
                    .ConfigureAwait(false);

                await _context
                    .SendConnectionKeepAliveMessageAsync(_cts.Token)
                    .ConfigureAwait(false);
            }
        }
    }
}

#endif
