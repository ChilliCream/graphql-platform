namespace Mocha.Testing.Internal;

/// <summary>
/// Fluent builder for configuring stub responses for a specific message type.
/// </summary>
/// <typeparam name="T">The message type to stub.</typeparam>
internal sealed class MessageStubBuilder<T> : IMessageStubBuilder<T>
{
    private readonly StubRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageStubBuilder{T}"/> class.
    /// </summary>
    /// <param name="registry">The stub registry to register responses in.</param>
    public MessageStubBuilder(StubRegistry registry) => _registry = registry;

    /// <inheritdoc />
    public void RespondWith<TResponse>(Func<T, TResponse> factory)
        => _registry.Register(factory);

    /// <inheritdoc />
    public void RespondWith<TResponse>(TResponse response)
        => _registry.Register<T, TResponse>(_ => response);
}
