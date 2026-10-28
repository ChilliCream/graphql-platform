using NATS.Client.Core;

namespace Mocha.Transport.Nats;

/// <summary>
/// Supplies the NATS connection the transport publishes and consumes over.
/// </summary>
public interface INatsConnectionProvider
{
    /// <summary>
    /// Gets the hostname of the NATS server.
    /// </summary>
    string Host { get; }

    /// <summary>
    /// Gets the port of the NATS server.
    /// </summary>
    int Port { get; }

    /// <summary>
    /// Gets the connection used by the transport.
    /// </summary>
    INatsConnection Connection { get; }
}

/// <summary>
/// Adapts an <see cref="INatsConnection"/> resolved from dependency injection into the
/// <see cref="INatsConnectionProvider"/> abstraction.
/// </summary>
public sealed class NatsConnectionProvider : INatsConnectionProvider
{
    /// <summary>
    /// The port used when a configured server URL omits one.
    /// </summary>
    public const int DefaultPort = 4222;

    /// <summary>
    /// Creates a provider over the specified connection.
    /// </summary>
    /// <param name="connection">The connection to expose.</param>
    public NatsConnectionProvider(INatsConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        Connection = connection;

        var (host, port) = ParseFirstServer(connection.Opts.Url);
        Host = host;
        Port = port;
    }

    /// <inheritdoc />
    public string Host { get; }

    /// <inheritdoc />
    public int Port { get; }

    /// <inheritdoc />
    public INatsConnection Connection { get; }

    /// <summary>
    /// Extracts the host and port of the first server in a NATS connection string.
    /// </summary>
    /// <param name="url">The connection string, which may list several comma-separated servers.</param>
    /// <returns>The host and port of the first entry.</returns>
    public static (string Host, int Port) ParseFirstServer(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        var first = url.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];

        if (!first.Contains("://", StringComparison.Ordinal))
        {
            first = "nats://" + first;
        }

        if (!Uri.TryCreate(first, UriKind.Absolute, out var uri))
        {
            throw ThrowHelper.InvalidServerUrl(url);
        }

        return (uri.Host, uri.Port < 0 ? DefaultPort : uri.Port);
    }
}
