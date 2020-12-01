using System;
using Microsoft.Extensions.Options;

namespace StrawberryShake.Transport.WebSockets
{
    public class DefaultWebSocketClientFactory
        : IWebSocketClientFactory
    {
        private readonly IOptionsMonitor<WebSocketClientFactoryOptions> _optionsMonitor;

        public DefaultWebSocketClientFactory(
            IOptionsMonitor<WebSocketClientFactoryOptions> optionsMonitor)
        {
            _optionsMonitor = optionsMonitor;
        }

        public WebSocketClient CreateClient(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The web socket name cannot be null or empty.",
                    nameof(name));
            }

            var client = new WebSocketClient();
            WebSocketClientFactoryOptions options = _optionsMonitor.Get(name);

            for (var i = 0; i < options.WebSocketClientActions.Count; i++)
            {
                options.WebSocketClientActions[i](client);
            }

            return client;
        }
    }
}
