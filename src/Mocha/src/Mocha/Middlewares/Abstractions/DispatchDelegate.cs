namespace Mocha.Middlewares;

/// <summary>
/// Represents a step in the dispatch middleware pipeline that processes an outgoing message.
/// </summary>
/// <param name="context">The dispatch context containing the message and dispatch metadata.</param>
/// <returns>A <see cref="ValueTask"/> representing the asynchronous dispatch operation.</returns>
public delegate ValueTask DispatchDelegate(IDispatchContext context);
