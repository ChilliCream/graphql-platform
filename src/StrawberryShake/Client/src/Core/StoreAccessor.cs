using StrawberryShake.Properties;

namespace StrawberryShake;

/// <summary>
/// The store accessor allows access to the stores and additionally some helpers for
/// serialization/deserialization of store objects.
/// </summary>
public abstract class StoreAccessor : IStoreAccessor
{
    private readonly Dictionary<Type, IOperationRequestFactory> _requestFactories;
    private readonly Dictionary<Type, IOperationResultDataFactory> _resultDataFactories;

    protected StoreAccessor(
        IOperationStore operationStore,
        IEntityStore entityStore,
        IEntityIdSerializer entityIdSerializer,
        IEnumerable<IOperationRequestFactory> operationRequestFactories,
        IEnumerable<IOperationResultDataFactory> operationResultDataFactories)
    {
        if (operationRequestFactories is null)
        {
            throw new ArgumentNullException(nameof(operationRequestFactories));
        }

        if (operationResultDataFactories is null)
        {
            throw new ArgumentNullException(nameof(operationResultDataFactories));
        }

        OperationStore = operationStore ??
            throw new ArgumentNullException(nameof(operationStore));
        EntityStore = entityStore ??
            throw new ArgumentNullException(nameof(entityStore));
        EntityIdSerializer = entityIdSerializer ??
            throw new ArgumentNullException(nameof(entityIdSerializer));
        _requestFactories = operationRequestFactories.ToDictionary(t => t.ResultType);
        _resultDataFactories = operationResultDataFactories.ToDictionary(t => t.ResultType);
    }

    /// <summary>
    /// Gets the operation store tracks and stores results by requests.
    /// </summary>
    public IOperationStore OperationStore { get; }

    /// <summary>
    /// Get the entity store tracks and stores the GraphQL entities.
    /// </summary>
    public IEntityStore EntityStore { get; }

    /// <summary>
    /// Gets the entity ID serializer.
    /// </summary>
    public IEntityIdSerializer EntityIdSerializer { get; }

    /// <summary>
    /// Gets the operation request factory to recreate a request..
    /// </summary>
    /// <param name="resultType">
    /// The request result type.
    /// </param>
    /// <returns>
    /// Returns a factory that can create requests.
    /// </returns>
    public IOperationRequestFactory GetOperationRequestFactory(Type resultType)
    {
        if (resultType is null)
        {
            throw new ArgumentNullException(nameof(resultType));
        }

        if (_requestFactories.TryGetValue(resultType, out var requestFactory))
        {
            return requestFactory;
        }

        throw new ArgumentException(
            Resources.StoreAccessor_GetOperationRequestFactory_InvalidResultType,
            nameof(resultType));
    }

    /// <summary>
    /// Gets the result data factory for the specified result type.
    /// </summary>
    /// <param name="resultType">The result type.</param>
    /// <returns>
    /// Returns an instance of <see cref="IOperationResultDataFactory"/> for the
    /// specified <paramref name="resultType"/>.
    /// </returns>
    public IOperationResultDataFactory GetOperationResultDataFactory(Type resultType)
    {
        if (resultType is null)
        {
            throw new ArgumentNullException(nameof(resultType));
        }

        if (_resultDataFactories.TryGetValue(resultType, out var resultDataFactory))
        {
            return resultDataFactory;
        }

        throw new ArgumentException(
            Resources.StoreAccessor_GetOperationRequestFactory_InvalidResultType,
            nameof(resultType));
    }
}
