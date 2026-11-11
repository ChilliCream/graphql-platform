using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Mocha.Transport.RabbitMQ.Tests.Helpers;

/// <summary>
/// A TCP proxy in front of the broker that can drop its connections, and can accept new connections
/// without answering them.
/// </summary>
public sealed class TcpProxy : IAsyncDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _stop = new();
    private readonly ConcurrentDictionary<TcpClient, TcpClient> _sessions = new();
    private readonly string _brokerHost;
    private readonly int _brokerPort;
    private readonly Task _accept;
    private int _unanswered;

    public TcpProxy(string brokerHost, int brokerPort)
    {
        _brokerHost = brokerHost;
        _brokerPort = brokerPort;
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _accept = AcceptAsync();
    }

    /// <summary>
    /// Gets the local port the proxy listens on.
    /// </summary>
    public int Port { get; }

    /// <summary>
    /// Gets or sets whether new connections are accepted and then never answered.
    /// </summary>
    public bool IsSilent { get; set; }

    /// <summary>
    /// Gets the number of connections that were accepted and not answered.
    /// </summary>
    public int UnansweredConnections => _unanswered;

    /// <summary>
    /// Closes every connection through the proxy.
    /// </summary>
    public void DropConnections()
    {
        foreach (var (client, upstream) in _sessions)
        {
            client.Close();
            upstream.Close();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        _listener.Stop();
        DropConnections();
        await _accept;
        _stop.Dispose();
    }

    private async Task AcceptAsync()
    {
        var sessions = new List<Task>();
        try
        {
            while (true)
            {
                var client = await _listener.AcceptTcpClientAsync(_stop.Token);
                sessions.Add(ForwardAsync(client));
            }
        }
        catch (Exception ex)
            when (ex is OperationCanceledException or SocketException or ObjectDisposedException
                or InvalidOperationException)
        {
            // The proxy was stopped.
        }

        await Task.WhenAll(sessions);
    }

    private async Task ForwardAsync(TcpClient client)
    {
        var upstream = new TcpClient();
        _sessions[client] = upstream;
        try
        {
            if (IsSilent)
            {
                // The connection stays open, and the peer never receives a byte.
                Interlocked.Increment(ref _unanswered);
                await Task.Delay(Timeout.Infinite, _stop.Token);
            }

            await upstream.ConnectAsync(_brokerHost, _brokerPort, _stop.Token);
            var toBroker = client.GetStream().CopyToAsync(upstream.GetStream(), _stop.Token);
            var toClient = upstream.GetStream().CopyToAsync(client.GetStream(), _stop.Token);
            await Task.WhenAny(toBroker, toClient);
        }
        catch (Exception ex)
            when (ex is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
        {
            // The session ended.
        }
        finally
        {
            _sessions.TryRemove(client, out _);
            client.Dispose();
            upstream.Dispose();
        }
    }
}
