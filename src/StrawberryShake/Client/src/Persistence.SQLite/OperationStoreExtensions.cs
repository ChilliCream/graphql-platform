using System.Reflection;

namespace StrawberryShake.Persistence.SQLite;

internal static class OperationStoreExtensions
{
    private static readonly MethodInfo s_setGeneric = typeof(IOperationStore)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public)
        .First(t =>
            t.IsGenericMethodDefinition
            && t.Name.Equals(nameof(IOperationStore.Set), StringComparison.Ordinal));

    /// <summary>
    /// Stores the <paramref name="operationResult"/> for the specified
    /// <paramref name="operationRequest"/>.
    /// </summary>
    /// <param name="operationStore">
    /// The operation store.
    /// </param>
    /// <param name="operationRequest">
    /// The operation request for which a result shall be stored.
    /// </param>
    /// <param name="operationResult">
    /// The operation result that shall be stored.
    /// </param>
    public static void Set(
        this IOperationStore operationStore,
        OperationRequest operationRequest,
        IOperationResult operationResult)
    {
        s_setGeneric
            .MakeGenericMethod(operationResult.DataType)
            .Invoke(operationStore, [operationRequest, operationResult]);
    }
}
