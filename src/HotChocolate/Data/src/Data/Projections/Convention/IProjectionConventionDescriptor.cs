namespace HotChocolate.Data.Projections
{
    /// <summary>
    /// This descriptor is used to configure a <see cref="ProjectionConvention"/>.
    /// </summary>
    public interface IProjectionConventionDescriptor
    {
        IProjectionConventionDescriptor RegisterFieldHandler<THandler>()
            where THandler : IProjectionFieldHandler;

        IProjectionConventionDescriptor RegisterFieldHandler<THandler>(THandler handler)
            where THandler : IProjectionFieldHandler;

        IProjectionConventionDescriptor RegisterFieldInterceptor<THandler>()
            where THandler : IProjectionFieldInterceptor;

        IProjectionConventionDescriptor RegisterFieldInterceptor<THandler>(THandler handler)
            where THandler : IProjectionFieldInterceptor;

        IProjectionConventionDescriptor RegisterOptimizer<THandler>()
            where THandler : IProjectionOptimizer;

        IProjectionConventionDescriptor RegisterOptimizer<THandler>(THandler handler)
            where THandler : IProjectionOptimizer;
    }
}
