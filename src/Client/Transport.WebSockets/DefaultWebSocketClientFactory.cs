using System;
using System.Net.WebSockets;
using Microsoft.Extensions.Options;

namespace StrawberryShake.Transport.WebSockets
{
    public class DefaultWebSocketClientFactory
        : IWebSocketClientFactory
    {
        private readonly IOptionsMonitor<ClientWebSocketFactoryOptions> _optionsMonitor;

        public DefaultWebSocketClientFactory(
            IOptionsMonitor<ClientWebSocketFactoryOptions> optionsMonitor)
        {
            _optionsMonitor = optionsMonitor;
        }

        public ClientWebSocket CreateClient(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The web socket name cannot be null or empty.",
                    nameof(name));
            }

            var webSocket = new ClientWebSocket();
            ClientWebSocketFactoryOptions options = _optionsMonitor.Get(name);

            for (var i = 0; i < options.ClientWebSocketActions.Count; i++)
            {
                options.ClientWebSocketActions[i](webSocket);
            }

            return webSocket;
        }
    }
}
