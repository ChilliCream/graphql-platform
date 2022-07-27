using HotChocolate.Data.MongoDb.Filters;
using HotChocolate.Data.MongoDb.Projections;
using HotChocolate.Data.MongoDb.Sorting;

namespace HotChocolate.Data.MongoDb;

/// <summary>
/// Provides mongo extensions for the <see cref="ISchemaBuilder"/>.
/// </summary>
public static class MongoDbSchemaBuilderExtensions
{
    /// <summary>
    /// Adds filtering support.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    /// <param name="configure">
    /// The configuration of the filter convention
    /// </param>
    /// <param name="name"></param>
    /// <param name="compatabilityMode">Uses the old behaviour of naming the filters</param>
    /// <returns>
    /// Returns the <see cref="ISchemaBuilder"/>.
    /// </returns>
    public static ISchemaBuilder AddMongoDbFiltering(
        this ISchemaBuilder builder,
        Action<IMongoDbFilterConventionDescriptor>? configure = null,
        string? name = null,
        bool compatabilityMode = false) =>
        builder.AddFiltering(x =>
            {
                x.AddMongoDbDefaults(compatabilityMode);
                configure?.Invoke(new MongoDbFilterConventionDescriptor(x));
            },
            name);

    /// <summary>
    /// Adds sorting support.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    /// <param name="configure">
    /// The configuration of the sort convention
    /// </param>
    /// <param name="name"></param>
    /// <returns>
    /// Returns the <see cref="ISchemaBuilder"/>.
    /// </returns>
    public static ISchemaBuilder AddMongoDbSorting(
        this ISchemaBuilder builder,
        Action<IMongoSortConventionDescriptor>? configure = null,
        string? name = null) =>
        builder.AddSorting(x =>
            {
                x.AddMongoDbDefaults();
                configure?.Invoke(new MongoSortConventionDescriptor(x));
            },
            name);

    /// <summary>
    /// Adds projections support.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    /// <param name="configure">
    /// The configuration of the projection convention
    /// </param>
    /// <param name="name"></param>
    /// <returns>
    /// Returns the <see cref="ISchemaBuilder"/>.
    /// </returns>
    public static ISchemaBuilder AddMongoDbProjections(
        this ISchemaBuilder builder,
        Action<IMongoProjectionConventionDescriptor>? configure = null,
        string? name = null) =>
        builder.AddProjections(x =>
            {
                x.AddMongoDbDefaults();
                configure?.Invoke(new MongoProjectionConventionDescriptor(x));
            },
            name);
}
