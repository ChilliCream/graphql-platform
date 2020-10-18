namespace HotChocolate.Data.Projections
{
    /// <summary>
    /// This descriptor is used to configure a <see cref="ProjectionProvider"/>.
    /// </summary>
    public interface IProjectionProviderDescriptor
    {
        /// <summary>
        /// Registers a field handler that is used to project selections to the database
        /// This field handler is either injected by the dependency injection or created by an
        /// activator
        /// </summary>
        /// <typeparam name="THandler">The type of the field handler</typeparam>
        /// <returns>The descriptor that this methods was called on</returns>
        IProjectionProviderDescriptor RegisterFieldHandler<THandler>()
            where THandler : IProjectionFieldHandler;

        /// <summary>
        /// Registers an instance of a field handler that is used to project selections to the
        /// database. This instance is directly used by the visitor for projection the selection set
        /// </summary>
        /// <typeparam name="THandler">The type of the field handler</typeparam>
        /// <returns>The descriptor that this methods was called on</returns>
        IProjectionProviderDescriptor RegisterFieldHandler<THandler>(THandler handler)
            where THandler : IProjectionFieldHandler;

        /// <summary>
        /// Registers a field interceptor that is used to intercept the projection of a field
        /// This can be used to emulate middlewares like UseFiltering and modify the context
        /// before the actual field projection happens
        /// </summary>
        /// <typeparam name="THandler">The type of the field interceptor</typeparam>
        /// <returns>The descriptor that this methods was called on</returns>
        IProjectionProviderDescriptor RegisterFieldInterceptor<THandler>()
            where THandler : IProjectionFieldInterceptor;

        /// <summary>
        /// Registers an instance of a field interceptor that is used to intercept the projection
        /// of a field. This can be used to emulate middlewares like UseFiltering and modify
        /// the context before the actual field projection happens.
        /// This instance is directly used by the visitor for projection the selection set
        /// </summary>
        /// <typeparam name="THandler">The type of the field interceptor</typeparam>
        /// <returns>The descriptor that this methods was called on</returns>
        IProjectionProviderDescriptor RegisterFieldInterceptor<THandler>(THandler handler)
            where THandler : IProjectionFieldInterceptor;

        /// <summary>
        /// Registers a field optimizer that is used to optimize a selection set before it
        /// is projected. With optimizers you can delete, add or rewrite fields on the selection
        /// set. This can also be used to rewrite the resolver pipeline.
        /// </summary>
        /// <typeparam name="THandler">The type of the field optimizer</typeparam>
        /// <returns>The descriptor that this methods was called on</returns>
        IProjectionProviderDescriptor RegisterOptimizer<THandler>()
            where THandler : IProjectionOptimizer;

        /// <summary>
        /// Registers a instance of an optimizer that is used to optimize a selection set before it
        /// is projected. With optimizers you can delete, add or rewrite fields on the selection
        /// set. This can also be used to rewrite the resolver pipeline.
        /// This instance is directly used by the visitor for projection the selection set
        /// </summary>
        /// <param name="handler"></param>
        /// <typeparam name="THandler"></typeparam>
        /// <returns></returns>
        IProjectionProviderDescriptor RegisterOptimizer<THandler>(THandler handler)
            where THandler : IProjectionOptimizer;
    }
}
