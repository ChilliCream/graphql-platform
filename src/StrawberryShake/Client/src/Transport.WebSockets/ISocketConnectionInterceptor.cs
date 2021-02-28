using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.Transport.WebSockets
{
    /// <summary>
    /// Intercepts connections of a <see cref="ISocketProtocol"/>
    /// </summary>
    public interface ISocketConnectionInterceptor
    {
        /// <summary>
        /// This method is called before the first message is sent to the server. The object
        /// returned from this message, will be passed to the server as the initial payload.
        /// </summary>
        /// <param name="protocol">
        /// The protocol over which the connection will be established
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token of this connection
        /// </param>
        /// <returns>
        /// Returns the initial payload of the connection
        /// </returns>
        public ValueTask<object?> CreateConnectionInitPayload(
            ISocketProtocol protocol,
            CancellationToken cancellationToken);
    }
}
