using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.Transport.WebSockets
{
    internal class DefaultSocketConnectionInterceptor : ISocketConnectionInterceptor
    {
        public ValueTask<object?> CreateConnectionInitPayload(
            ISocketProtocol protocol,
            CancellationToken cancellationToken)
        {
            return default;
        }

        public static readonly DefaultSocketConnectionInterceptor Instance = new();
    }
}
