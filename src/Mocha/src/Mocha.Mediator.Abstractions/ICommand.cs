namespace Mocha.Mediator;

/// <summary>
/// Defines a marker for a command that does not return a response.
/// </summary>
public interface ICommand;

/// <summary>
/// Defines a marker for a command that returns a response of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface ICommand<out TResponse>;
