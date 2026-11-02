using System.IO.Pipes;
using System.Net.Sockets;
using System.Security.AccessControl;
using System.Security.Principal;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// A peer endpoint listening the way a Venti session's does: a Unix domain socket in the peer
/// registry directory, or a named pipe on Windows.
/// </summary>
internal sealed class VentiPeerTestEndpoint : IDisposable
{
    private static readonly TimeSpan s_acceptTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan s_connectionGrace = TimeSpan.FromMilliseconds(100);

    private readonly Socket? _socket;
    private readonly NamedPipeServerStream? _pipe;
    private readonly Task? _pipeConnection;

    private VentiPeerTestEndpoint(string address, Socket socket)
    {
        Address = address;
        _socket = socket;
    }

    private VentiPeerTestEndpoint(string address, NamedPipeServerStream pipe)
    {
        Address = address;
        _pipe = pipe;
        _pipeConnection = pipe.WaitForConnectionAsync();
    }

    /// <summary>
    /// The endpoint address a Venti registry file lists as <c>messagingSocketPath</c>.
    /// </summary>
    public string Address { get; }

    /// <summary>
    /// Starts listening for one connection. The Unix domain socket is created in
    /// <paramref name="peerDirectory"/>.
    /// </summary>
    public static VentiPeerTestEndpoint Listen(string peerDirectory)
    {
        if (OperatingSystem.IsWindows())
        {
            var pipeName = "nitro-venti-test-" + Guid.NewGuid().ToString("N");
            var pipe = new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

            return new VentiPeerTestEndpoint(@"\\.\pipe\" + pipeName, pipe);
        }

        var socketPath = Path.Combine(peerDirectory, "0011223344556677.sock");
        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        socket.Bind(new UnixDomainSocketEndPoint(socketPath));
        socket.Listen(1);

        return new VentiPeerTestEndpoint(socketPath, socket);
    }

    /// <summary>
    /// Returns the stream of the first connection, or throws <see cref="TimeoutException"/> when no
    /// peer connects within ten seconds.
    /// </summary>
    public async Task<Stream> AcceptAsync(CancellationToken cancellationToken)
    {
        if (_socket is not null)
        {
            var peer = await _socket.AcceptAsync(cancellationToken).AsTask()
                .WaitAsync(s_acceptTimeout, cancellationToken);

            return new NetworkStream(peer, ownsSocket: true);
        }

        await _pipeConnection!.WaitAsync(s_acceptTimeout, cancellationToken);
        return _pipe!;
    }

    /// <summary>
    /// Connects to the endpoint as a peer client does and returns the client's stream.
    /// </summary>
    public async Task<Stream> ConnectAsync(CancellationToken cancellationToken)
    {
        if (_socket is not null)
        {
            var client = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            await client.ConnectAsync(new UnixDomainSocketEndPoint(Address), cancellationToken);
            return new NetworkStream(client, ownsSocket: true);
        }

        var pipe = new NamedPipeClientStream(
            ".",
            Address[@"\\.\pipe\".Length..],
            PipeDirection.InOut,
            PipeOptions.Asynchronous);
        await pipe.ConnectAsync(cancellationToken);
        return pipe;
    }

    /// <summary>
    /// Whether a peer has connected, waiting up to a tenth of a second for one.
    /// </summary>
    public async Task<bool> HasConnectionAsync()
    {
        if (_socket is not null)
        {
            return _socket.Poll(s_connectionGrace, SelectMode.SelectRead);
        }

        return await Task.WhenAny(_pipeConnection!, Task.Delay(s_connectionGrace)) == _pipeConnection;
    }

    /// <summary>
    /// Writes a key file the way Venti does: with <paramref name="unixMode"/> on Unix, or on
    /// Windows with an access control list that grants only the current user access.
    /// </summary>
    public static async Task WriteKeyFileAsync(
        string path,
        string json,
        UnixFileMode unixMode,
        CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(path, json, cancellationToken);

        if (OperatingSystem.IsWindows())
        {
            using var identity = WindowsIdentity.GetCurrent();
            var security = new FileSecurity();
            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            security.AddAccessRule(
                new FileSystemAccessRule(identity.User!, FileSystemRights.FullControl, AccessControlType.Allow));
            new FileInfo(path).SetAccessControl(security);
        }
        else
        {
            File.SetUnixFileMode(path, unixMode);
        }
    }

    public void Dispose()
    {
        _socket?.Dispose();
        _pipe?.Dispose();
    }
}
