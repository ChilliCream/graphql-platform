namespace Mocha;

/// <summary>
/// Represents a step in the consumer middleware pipeline that processes a message for a specific consumer.
/// </summary>
/// <param name="context">The consume context containing the message and consumer metadata.</param>
/// <returns>A <see cref="ValueTask"/> representing the asynchronous consume operation.</returns>
public delegate ValueTask ConsumerDelegate(IConsumeContext context);
