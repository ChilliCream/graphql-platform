using System.Collections.Concurrent;

namespace Mocha.Testing.Internal;

/// <summary>
/// Stores stub response factories for sent messages, keyed by message type.
/// </summary>
internal sealed class StubRegistry
{
    private readonly ConcurrentDictionary<Type, Func<object, object>> _stubs = new();

    /// <summary>
    /// Registers a stub response factory for the specified message type.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="factory">A factory that produces a response from the incoming message.</param>
    public void Register<T, TResponse>(Func<T, TResponse> factory)
    {
        _stubs[typeof(T)] = msg => factory((T)msg)!;
    }

    /// <summary>
    /// Attempts to retrieve a stub response factory for the specified message type.
    /// </summary>
    /// <param name="messageType">The message type to look up.</param>
    /// <param name="factory">When this method returns, contains the factory if found; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if a stub was found; otherwise, <c>false</c>.</returns>
    public bool TryGetStub(Type messageType, out Func<object, object>? factory)
    {
        return _stubs.TryGetValue(messageType, out factory);
    }
}
