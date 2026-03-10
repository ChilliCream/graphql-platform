using System.Reflection;

namespace StrawberryShake.Persistence.SQLite;

/// <summary>
/// This is a helper class that provides generic and non-generic factory methods to create
/// a new instance of <see cref="OperationResult{T}"/>.
/// </summary>
internal static class OperationResult
{
    private static readonly MethodInfo s_factory =
        typeof(OperationResult)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m =>
                m.IsGenericMethodDefinition
                && m.Name.Equals(nameof(Create), StringComparison.Ordinal));

    public static IOperationResult Create(
        object? data,
        Type dataType,
        IOperationResultDataInfo? dataInfo,
        IOperationResultDataFactory dataFactory,
        IReadOnlyList<IClientError>? errors,
        IReadOnlyDictionary<string, object?>? extensions = null,
        IReadOnlyDictionary<string, object?>? contextData = null)
    {
        return (IOperationResult)s_factory
            .MakeGenericMethod(dataType)
            .Invoke(
                null,
                [data, dataInfo, dataFactory, errors, extensions, contextData])!;
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
