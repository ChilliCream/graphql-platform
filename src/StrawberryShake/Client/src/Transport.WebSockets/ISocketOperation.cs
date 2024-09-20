using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Transport.WebSockets;

/// <summary>
/// Represents a operation on a socket
/// </summary>
public interface ISocketOperation : IAsyncDisposable
{
    /// <summary>
    /// The id of the operation
    /// </summary>
    string Id { get; }

    /// <summary>
    /// CRate an operation message stream.
    /// </summary>
    IAsyncEnumerable<OperationMessage> ReadAsync();

    /// <summary>
    /// Complete the operation
    /// </summary>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> to cancel the completion
    /// </param>
    /// <returns>A task that is completed once the operation is completed</returns>
    ValueTask CompleteAsync(CancellationToken cancellationToken);
}
