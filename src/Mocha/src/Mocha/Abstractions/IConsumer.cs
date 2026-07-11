namespace Mocha;

/// <summary>
/// Interface for consumers that receive a full <see cref="IConsumeContext{TMessage}"/>
/// instead of just the deserialized message.
/// </summary>
public interface IConsumer<in TMessage> : IConsumer
{
    ValueTask ConsumeAsync(IConsumeContext<TMessage> context);

    static Type IHandler.EventType => typeof(TMessage);
}

public interface IConsumer : IHandler
{
    static Type? IHandler.ResponseType => null;

    static Type? IHandler.RequestType => null;
}
