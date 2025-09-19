namespace StrawberryShake;

/// <summary>
/// The store accessor allows access to the stores and additionally some helpers for
/// serialization/deserialization of store objects.
/// </summary>
public interface IStoreAccessor
{
    /// <summary>
    /// Gets the operation store tracks and stores results by requests.
    /// </summary>
    IOperationStore OperationStore { get; }

    /// <summary>
    /// Get the entity store tracks and stores the GraphQL entities.
    /// </summary>
    IEntityStore EntityStore { get; }

    /// <summary>
    /// Gets the entity ID serializer.
    /// </summary>
    IEntityIdSerializer EntityIdSerializer { get; }

    /// <summary>
    /// Gets the operation request factory to recreate a request..
    /// </summary>
    /// <param name="resultType">
    /// The request result type.
    /// </param>
    /// <returns>
    /// Returns a factory that can create requests.
    /// </returns>
    IOperationRequestFactory GetOperationRequestFactory(Type resultType);

    /// <summary>
    /// Gets the result data factory for the specified result type.
    /// </summary>
    /// <param name="resultType">The result type.</param>
    /// <returns>
    /// Returns an instance of <see cref="IOperationResultDataFactory"/> for the
    /// specified <paramref name="resultType"/>.
    /// </returns>
    IOperationResultDataFactory GetOperationResultDataFactory(Type resultType);
}
