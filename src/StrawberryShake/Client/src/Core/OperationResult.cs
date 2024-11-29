using System.Collections.Immutable;
using System.Reflection;
using static StrawberryShake.Properties.Resources;

namespace StrawberryShake;

/// <summary>
/// Represents a GraphQL result object.
/// </summary>
/// <typeparam name="T">
/// The type of the data object.
/// </typeparam>
public sealed class OperationResult<T> : IOperationResult<T> where T : class
{
    public OperationResult(
        T? data,
        IOperationResultDataInfo? dataInfo,
        IOperationResultDataFactory<T> dataFactory,
        IReadOnlyList<IClientError>? errors,
        IReadOnlyDictionary<string, object?>? extensions = null,
        IReadOnlyDictionary<string, object?>? contextData = null)
    {
        if (data is null && errors is null)
        {
            throw new ArgumentNullException(nameof(data), Response_BodyAndExceptionAreNull);
        }

        Data = data;
        DataInfo = dataInfo;
        DataFactory = dataFactory;
        Errors = errors ?? Array.Empty<IClientError>();
        Extensions = extensions ?? ImmutableDictionary<string, object?>.Empty;
        ContextData = contextData ?? ImmutableDictionary<string, object?>.Empty;
    }

    /// <summary>
    /// The GraphQL data object.
    /// </summary>
    public T? Data { get; }

    object? IOperationResult.Data => Data;

    Type IOperationResult.DataType => typeof(T);

    /// <summary>
    /// The data info contains the reference list to entities and the store version of
    /// which this result was created.
    /// </summary>
    public IOperationResultDataInfo? DataInfo { get; }

    /// <summary>
    /// The data factory can be used to create a new data object from the result info.
    /// </summary>
    public IOperationResultDataFactory<T> DataFactory { get; }

    object IOperationResult.DataFactory => DataFactory;

    /// <summary>
    /// Gets the GraphQL server errors.
    /// </summary>
    public IReadOnlyList<IClientError> Errors { get; }

    /// <summary>
    /// Gets additional custom data provided by the server.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Extensions { get; }

    /// <summary>
    /// Gets additional custom data provided by client extensions.
    /// </summary>
    public IReadOnlyDictionary<string, object?> ContextData { get; }

    /// <summary>
    /// Creates a new result version with the specified data object.
    /// </summary>
    /// <param name="data">
    /// The data object.
    /// </param>
    /// <param name="dataInfo">
    /// The data info.
    /// </param>
    /// <returns></returns>
    public IOperationResult<T> WithData(T data, IOperationResultDataInfo dataInfo)
        => new OperationResult<T>(data, dataInfo, DataFactory, Errors, Extensions, ContextData);
}

/// <summary>
/// This is a helper class that provides generic and non-generic factory methods to create
/// a new instance of <see cref="OperationResult{T}"/>.
/// </summary>
public static class OperationResult
{
    private static readonly MethodInfo _factory =
        typeof(OperationResult)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m =>
                m.IsGenericMethodDefinition &&
                m.Name.Equals(nameof(Create), StringComparison.Ordinal));

    public static IOperationResult Create(
        object? data,
        Type dataType,
        IOperationResultDataInfo? dataInfo,
        IOperationResultDataFactory dataFactory,
        IReadOnlyList<IClientError>? errors,
        IReadOnlyDictionary<string, object?>? extensions = null,
        IReadOnlyDictionary<string, object?>? contextData = null)
    {
        return (IOperationResult)_factory
            .MakeGenericMethod(dataType)
            .Invoke(
                null,
                [data, dataInfo, dataFactory, errors, extensions, contextData,])!;
    }

    public static IOperationResult<TData> Create<TData>(
        TData? data,
        IOperationResultDataInfo? dataInfo,
        IOperationResultDataFactory<TData> dataFactory,
        IReadOnlyList<IClientError>? errors,
        IReadOnlyDictionary<string, object?>? extensions = null,
        IReadOnlyDictionary<string, object?>? contextData = null)
        where TData : class =>
        new OperationResult<TData>(
            data,
            dataInfo,
            dataFactory,
            errors,
            extensions,
            contextData);
}
