using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Transport.WebSockets
{
    /// <summary>
    /// Represents a operation on a socket
    /// </summary>
    public interface ISocketOperation : IAsyncDisposable
    {
        /// <summary>
        /// The id of the operation
        /// </summary>
        string Id { get; }

        IAsyncEnumerable<OperationMessage> ReadAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken);
    }
}
