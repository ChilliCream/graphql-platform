namespace Mocha.Mediator;

/// <summary>
/// Defines a handler for a command that does not return a response.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle.</typeparam>
public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    /// <summary>
    /// Handles the specified command.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask HandleAsync(TCommand command, CancellationToken cancellationToken);
}

/// <summary>
/// Defines a handler for a command that returns a response.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    /// <summary>
    /// Handles the specified command and returns a response.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="ValueTask{TResponse}"/> containing the response.</returns>
    ValueTask<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
