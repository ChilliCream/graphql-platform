namespace Mocha;

/// <summary>
/// Typed consume context that provides access to the deserialized message
/// alongside all envelope metadata and headers.
/// </summary>
public interface IConsumeContext<out TMessage> : IConsumeContext
{
    TMessage Message { get; }
}
