using HotChocolate;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides utility methods to setup dependency injection.
/// </summary>
public static class HotChocolateInMemoryPersistedOperationsRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds an in-memory operation document storage to the service collection.
    /// </summary>
    /// <param name="builder">
    /// The service collection to which the services are added.
    /// </param>
    public static IRequestExecutorBuilder AddInMemoryOperationDocumentStorage(
        this IRequestExecutorBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.ConfigureSchemaServices(
            s => s.AddInMemoryOperationDocumentStorage());
    }
}
