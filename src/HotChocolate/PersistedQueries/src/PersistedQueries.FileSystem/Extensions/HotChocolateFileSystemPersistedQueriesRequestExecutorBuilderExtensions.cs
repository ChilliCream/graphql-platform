using System;
using HotChocolate;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides utility methods to setup dependency injection.
/// </summary>
public static class HotChocolateFileSystemPersistedQueriesRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds a file system read and write query storage to the
    /// services collection.
    /// </summary>
    /// <param name="builder">
    /// The service collection to which the services are added.
    /// </param>
    /// <param name="cacheDirectory">
    /// The directory path that shall be used to store queries.
    /// </param>
    public static IRequestExecutorBuilder AddFileSystemQueryStorage(
        this IRequestExecutorBuilder builder,
        string? cacheDirectory = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.ConfigureSchemaServices(
            s => s.AddFileSystemOperationDocumentStorage(cacheDirectory));
    }

    /// <summary>
    /// Adds a file system read-only query storage to the
    /// services collection.
    /// </summary>
    /// <param name="builder">
    /// The service collection to which the services are added.
    /// </param>
    /// <param name="cacheDirectory">
    /// The directory path that shall be used to read queries from.
    /// </param>
    public static IRequestExecutorBuilder AddReadOnlyFileSystemQueryStorage(
        this IRequestExecutorBuilder builder,
        string? cacheDirectory = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.ConfigureSchemaServices(
            s => s.AddFileSystemOperationDocumentStorage(cacheDirectory));
    }
}