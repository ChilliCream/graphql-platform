using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Represents a step in the receive middleware pipeline that processes an incoming message.
/// </summary>
/// <param name="context">The receive context containing the envelope and receive metadata.</param>
/// <returns>A <see cref="ValueTask"/> representing the asynchronous receive operation.</returns>
public delegate ValueTask ReceiveDelegate(IReceiveContext context);
