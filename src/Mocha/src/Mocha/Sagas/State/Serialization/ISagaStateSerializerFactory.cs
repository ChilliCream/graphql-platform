namespace Mocha.Sagas;

/// <summary>
/// A factory that creates <see cref="ISagaStateSerializer"/> instances for specific saga state types.
/// </summary>
public interface ISagaStateSerializerFactory
{
    /// <summary>
    /// Gets a serializer for the specified saga state type.
    /// </summary>
    /// <param name="type">The CLR type of the saga state.</param>
    /// <returns>A serializer configured for the specified type.</returns>
    ISagaStateSerializer GetSerializer(Type type);
}
