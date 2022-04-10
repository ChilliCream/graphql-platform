using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Transport.Sockets.Client;

namespace HotChocolate.Stitching.Execution;

public interface ISocketClientFactory
{
    ValueTask<SocketClient> CreateClientAsync(string name, CancellationToken cancellationToken);
}
