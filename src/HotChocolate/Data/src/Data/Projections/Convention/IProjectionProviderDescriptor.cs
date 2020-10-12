namespace HotChocolate.Data.Projections
{
    /// <summary>
    /// This descriptor is used to configure a <see cref="ProjectionProvider"/>.
    /// </summary>
    public interface IProjectionProviderDescriptor
    {
        IProjectionProviderDescriptor RegisterFieldHandler<THandler>()
            where THandler : IProjectionFieldHandler;

        IProjectionProviderDescriptor RegisterFieldHandler<THandler>(THandler handler)
            where THandler : IProjectionFieldHandler;

        IProjectionProviderDescriptor RegisterFieldInterceptor<THandler>()
            where THandler : IProjectionFieldInterceptor;

        IProjectionProviderDescriptor RegisterFieldInterceptor<THandler>(THandler handler)
            where THandler : IProjectionFieldInterceptor;

        IProjectionProviderDescriptor RegisterOptimizer<THandler>()
            where THandler : IProjectionOptimizer;

        IProjectionProviderDescriptor RegisterOptimizer<THandler>(THandler handler)
            where THandler : IProjectionOptimizer;
    }
}
