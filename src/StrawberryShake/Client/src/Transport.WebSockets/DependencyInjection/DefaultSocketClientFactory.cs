using Microsoft.Extensions.Options;

namespace StrawberryShake.Transport.WebSockets;

/// <inheritdoc />
public class DefaultSocketClientFactory : ISocketClientFactory
{
    private readonly IOptionsMonitor<SocketClientFactoryOptions> _optionsMonitor;
    private readonly IReadOnlyList<ISocketProtocolFactory> _protocolFactories;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultSocketClientFactory"/>
    /// </summary>
    /// <param name="optionsMonitor">The options monitor for the factory options</param>
    /// <param name="protocolFactories">The possible protocol factories</param>
    public DefaultSocketClientFactory(
        IOptionsMonitor<SocketClientFactoryOptions> optionsMonitor,
        IEnumerable<ISocketProtocolFactory> protocolFactories)
    {
        _optionsMonitor = optionsMonitor ??
            throw new ArgumentNullException(nameof(optionsMonitor));
        _protocolFactories = protocolFactories?.ToArray() ??
            throw new ArgumentNullException(nameof(protocolFactories));
    }

    /// <inheritdoc />
    public ISocketClient CreateClient(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw ThrowHelper.Argument_IsNullOrEmpty(nameof(name));
        }

        var options = _optionsMonitor.Get(name);
        var client = new WebSocketClient(name, _protocolFactories);

        for (var i = 0; i < options.SocketClientActions.Count; i++)
        {
            options.SocketClientActions[i](client);
        }

        return client;
    }
}
