using HotChocolate.Data.MongoDb;
using HotChocolate.Data.MongoDb.Paging;
using HotChocolate.Data.MongoDb.Relay;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using MongoDB.Bson;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides data extensions for the <see cref="IRequestExecutorBuilder"/>.
/// </summary>
public static class MongoDbDataRequestBuilderExtensions
{
    /// <summary>
    /// Adds filtering support.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="name"></param>
    /// <param name="compatibilityMode">Uses the old behavior of naming the filters</param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    public static IRequestExecutorBuilder AddMongoDbFiltering(
        this IRequestExecutorBuilder builder,
        string? name = null,
        bool compatibilityMode = false) =>
        builder.ConfigureSchema(s => s.AddMongoDbFiltering(name, compatibilityMode));

    /// <summary>
    /// Adds sorting support.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="name"></param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    public static IRequestExecutorBuilder AddMongoDbSorting(
        this IRequestExecutorBuilder builder,
        string? name = null) =>
        builder.ConfigureSchema(s => s.AddMongoDbSorting(name));

    /// <summary>
    /// Adds projections support.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="name"></param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    public static IRequestExecutorBuilder AddMongoDbProjections(
        this IRequestExecutorBuilder builder,
        string? name = null) =>
        builder.ConfigureSchema(s => s.AddMongoDbProjections(name));

    /// <summary>
    /// Adds converter
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="compressGlobalIds">
    /// Compresses the global ids.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    public static IRequestExecutorBuilder AddObjectIdConverters(
        this IRequestExecutorBuilder builder,
        bool compressGlobalIds = true)
    {
        builder.AddNodeIdValueSerializer(
            new ObjectIdNodeIdValueSerializer(compressGlobalIds));

        return builder
            .BindRuntimeType<ObjectId, StringType>()
            .AddTypeConverter<ObjectId, string>(x => x.ToString())
            .AddTypeConverter<string, ObjectId>(x => new ObjectId(x));
    }

    /// <summary>
    /// Adds the MongoDB cursor and offset paging providers.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="providerName">
    /// The name which shall be used to refer to this registration.
    /// </param>
    /// <param name="defaultProvider">
    /// Defines if these providers shall be registered as default providers.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder for further configuration chaining.
    /// </returns>
    public static IRequestExecutorBuilder AddMongoDbPagingProviders(
        this IRequestExecutorBuilder builder,
        string? providerName = null,
        bool defaultProvider = false)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.AddCursorPagingProvider<MongoDbCursorPagingProvider>(
            providerName,
            defaultProvider);

        builder.AddOffsetPagingProvider<MongoDbOffsetPagingProvider>(
            providerName,
            defaultProvider);

        return builder;
    }
}
