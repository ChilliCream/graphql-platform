namespace Mocha.Mediator;

/// <summary>
/// Defines the contract for sending commands and queries to their respective single handlers.
/// </summary>
public interface ISender
{
    /// <summary>
    /// Sends a command that does not return a response.
    /// </summary>
    /// <param name="command">The command to send.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask SendAsync(ICommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a command that returns a response.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="command">The command to send.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="ValueTask{TResponse}"/> containing the response.</returns>
    ValueTask<TResponse> SendAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a query and returns the result.
    /// </summary>
    /// <typeparam name="TResponse">The type of the query result.</typeparam>
    /// <param name="query">The query to send.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="ValueTask{TResponse}"/> containing the result.</returns>
    ValueTask<TResponse> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message by its runtime type, returning the response as an <see cref="object"/>.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="ValueTask{T}"/> containing the response, or <see langword="null"/> for void commands.</returns>
    /// <remarks>
    /// The runtime type of <paramref name="message"/> must implement one of the following marker interfaces:
    /// <see cref="ICommand"/>, <see cref="ICommand{TResponse}"/>, or <see cref="IQuery{TResponse}"/>.
    /// An exception is thrown if the message does not implement a supported interface.
    /// </remarks>
    ValueTask<object?> SendAsync(object message, CancellationToken cancellationToken = default);
}
