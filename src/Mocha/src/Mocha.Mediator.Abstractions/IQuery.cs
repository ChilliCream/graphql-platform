namespace Mocha.Mediator;

/// <summary>
/// Defines a marker for a query that returns a response of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">The type of the query result.</typeparam>
public interface IQuery<out TResponse>;
