using HotChocolate.Data.MongoDb;
using HotChocolate.Data.MongoDb.Filters;
using HotChocolate.Data.MongoDb.Paging;
using HotChocolate.Data.MongoDb.Projections;
using HotChocolate.Data.MongoDb.Sorting;
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
    /// <param name="configure">
    /// The configuration of the filter convention
    /// </param>
    /// <param name="name"></param>
    /// <param name="compatabilityMode">Uses the old behaviour of naming the filters</param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    public static IRequestExecutorBuilder AddMongoDbFiltering(
        this IRequestExecutorBuilder builder,
        string? name = null,
        Action<IMongoDbFilterConventionDescriptor>? configure = null,
        bool compatabilityMode = false) =>
        builder.ConfigureSchema(s => s.AddMongoDbFiltering(configure, name, compatabilityMode));

    /// <summary>
    /// Adds sorting support.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="name"></param>
    /// <param name="configure">
    /// The configuration of the sort convention
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    public static IRequestExecutorBuilder AddMongoDbSorting(
        this IRequestExecutorBuilder builder,
        string? name = null,
        Action<IMongoSortConventionDescriptor>? configure = null) =>
        builder.ConfigureSchema(s => s.AddMongoDbSorting(configure, name));

    /// <summary>
    /// Adds projections support.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="configure">
    /// The configuration of the projection convention
    /// </param>
    /// <param name="name"></param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    public static IRequestExecutorBuilder AddMongoDbProjections(
        this IRequestExecutorBuilder builder,
        string? name = null,
        Action<IMongoProjectionConventionDescriptor>? configure = null) =>
        builder.ConfigureSchema(s => s.AddMongoDbProjections(configure, name));

    /// <summary>
    /// Adds converter
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    public static IRequestExecutorBuilder AddObjectIdConverters(
        this IRequestExecutorBuilder builder) =>
        builder
            .BindRuntimeType<ObjectId, StringType>()
#pragma warning disable CS8622
            .AddTypeConverter<ObjectId, string>(x => x.ToString())
            .AddTypeConverter<string, ObjectId>(x => new ObjectId(x));
#pragma warning restore CS8622

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
