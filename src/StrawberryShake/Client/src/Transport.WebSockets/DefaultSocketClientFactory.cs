using System;
using System.Linq;
using Microsoft.Extensions.Options;

namespace StrawberryShake.Transport.WebSockets
{
    public class DefaultSocketClientFactory
        : ISocketClientFactory
    {
        private readonly IOptionsMonitor<SocketClientFactoryOptions> _optionsMonitor;

        public DefaultSocketClientFactory(
            IOptionsMonitor<SocketClientFactoryOptions> optionsMonitor)
        {
            _optionsMonitor = optionsMonitor;
        }

        public ISocketClient CreateClient(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The web socket name cannot be null or empty.",
                    nameof(name));
            }

            SocketClientFactoryOptions options = _optionsMonitor.Get(name);
            var client = new WebSocketClient(name, options.Protocols.ToArray());

            for (var i = 0; i < options.WebSocketClientActions.Count; i++)
            {
                options.WebSocketClientActions[i](client);
            }

            return client;
        }
    }
}
