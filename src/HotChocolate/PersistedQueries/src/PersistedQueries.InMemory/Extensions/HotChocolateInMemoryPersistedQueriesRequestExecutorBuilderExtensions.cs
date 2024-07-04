using System;
using HotChocolate;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides utility methods to setup dependency injection.
/// </summary>
public static class HotChocolateInMemoryPersistedQueriesRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds a file system read and write query storage to the
    /// services collection.
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