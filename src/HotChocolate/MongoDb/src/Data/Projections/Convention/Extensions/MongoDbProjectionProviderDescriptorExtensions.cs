using HotChocolate.Data.MongoDb.Projections;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Projections.Handlers;
using HotChocolate.Data.Projections.Optimizers;

namespace HotChocolate.Data;

public static class MongoDbProjectionProviderDescriptorExtensions
{
    /// <summary>
    /// Initializes the default configuration for MongoDb on the convention by adding handlers
    /// </summary>
    /// <param name="descriptor">The descriptor where the handlers are registered</param>
    /// <returns>The <paramref name="descriptor"/></returns>
    public static IProjectionProviderDescriptor AddMongoDbDefaults(
        this IProjectionProviderDescriptor descriptor) =>
        descriptor.RegisterMongoDbHandlers();

    /// <summary>
    /// Registers projection handlers for mongodb
    /// </summary>
    /// <param name="descriptor">The descriptor where the handlers are registered</param>
    /// <returns>The <paramref name="descriptor"/></returns>
    /// <exception cref="ArgumentNullException">
    /// Throws in case the argument <paramref name="descriptor"/> is null
    /// </exception>
    public static IProjectionProviderDescriptor RegisterMongoDbHandlers(
        this IProjectionProviderDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        descriptor.RegisterFieldHandler(MongoDbProjectionScalarHandler.Create);
        descriptor.RegisterFieldHandler(MongoDbProjectionFieldHandler.Create);
        descriptor.RegisterOptimizer(QueryablePagingProjectionOptimizer.Create);
        descriptor.RegisterOptimizer(IsProjectedProjectionOptimizer.Create);
        return descriptor;
    }
}
