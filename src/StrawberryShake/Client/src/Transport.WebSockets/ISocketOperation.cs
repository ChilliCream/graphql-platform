using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;

namespace StrawberryShake.Transport.Subscriptions
{
    /// <summary>
    /// Represents a operation on a socket
    /// </summary>
    public interface ISocketOperation
        : IAsyncDisposable
    {
        /// <summary>
        /// The id of the operation
        /// </summary>
        string Id { get; }

        IAsyncEnumerable<JsonDocument> ReadAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken);
    }
}
