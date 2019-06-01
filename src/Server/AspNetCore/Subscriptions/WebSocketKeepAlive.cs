#if !ASPNETCLASSIC
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal sealed class WebSocketKeepAlive
    {
        private readonly int _keepAliveTimeout = 5000;

        private readonly IWebSocketContext _context;
        private readonly CancellationTokenSource _cts;

        public WebSocketKeepAlive(
            IWebSocketContext context,
            CancellationTokenSource cts)
        {
            _context = context;
            _cts = cts;
        }

        public void Start()
        {
            Task.Factory.StartNew(
                KeepConnectionAlive,
                _cts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        private async Task KeepConnectionAlive()
        {
            while (!_context.Closed || !_cts.IsCancellationRequested)
            {
                await Task.Delay(_keepAliveTimeout, _cts.Token)
                    .ConfigureAwait(false);
                await _context.SendConnectionKeepAliveMessageAsync(_cts.Token)
                    .ConfigureAwait(false);
            }
        }
    }
}

#endif
