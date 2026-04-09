namespace Mocha.Testing;

/// <summary>
/// Configures stub responses for messages of a specific type.
/// </summary>
/// <typeparam name="T">The message type to stub.</typeparam>
public interface IMessageStubBuilder<out T>
{
    /// <summary>
    /// Configures a response to be generated from the incoming message.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="factory">A factory that produces the response from the message.</param>
    void RespondWith<TResponse>(Func<T, TResponse> factory);

    /// <summary>
    /// Configures a fixed response to be returned.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="response">The response to return.</param>
    void RespondWith<TResponse>(TResponse response);
}
