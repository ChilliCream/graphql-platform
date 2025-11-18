using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Projections;

public class ProjectionProviderDescriptor : IProjectionProviderDescriptor
{
    protected ProjectionProviderDescriptor(IDescriptorContext context, string? scope)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Configuration.Scope = scope;
    }

    protected IDescriptorContext Context { get; }

    protected ProjectionProviderConfiguration Configuration { get; } = new();

    public ProjectionProviderConfiguration CreateConfiguration() => Configuration;

    /// <inheritdoc />
    public IProjectionProviderDescriptor RegisterFieldHandler<THandler>(
        Func<ProjectionProviderContext, THandler> factory)
        where THandler : IProjectionFieldHandler
    {
        // TODO: Find a better way
        Configuration.HandlerFactories.Add(ctx => factory(ctx));
        return this;
    }

    /// <inheritdoc />
    public IProjectionProviderDescriptor RegisterFieldHandler<THandler>(THandler handler)
        where THandler : IProjectionFieldHandler
    {
        // TODO: Find a better way
        Configuration.HandlerFactories.Add(_ => handler);
        return this;
    }

    /// <inheritdoc />
    public IProjectionProviderDescriptor RegisterFieldInterceptor<THandler>(
        Func<ProjectionProviderContext, THandler> factory)
        where THandler : IProjectionFieldInterceptor
    {
        // TODO: Find a better way
        Configuration.InterceptorFactories.Add(ctx => factory(ctx));
        return this;
    }

    /// <inheritdoc />
    public IProjectionProviderDescriptor RegisterFieldInterceptor<THandler>(THandler handler)
        where THandler : IProjectionFieldInterceptor
    {
        // TODO: Find a better way
        Configuration.InterceptorFactories.Add(_ => handler);
        return this;
    }

    /// <inheritdoc />
    public IProjectionProviderDescriptor RegisterOptimizer<THandler>(
        Func<ProjectionProviderContext, THandler> factory)
        where THandler : IProjectionOptimizer
    {
        // TODO: Find a better way
        Configuration.OptimizerFactories.Add(ctx => factory(ctx));
        return this;
    }

    /// <inheritdoc />
    public IProjectionProviderDescriptor RegisterOptimizer<THandler>(THandler handler)
        where THandler : IProjectionOptimizer
    {
        // TODO: Find a better way
        Configuration.OptimizerFactories.Add(_ => handler);
        return this;
    }

    /// <summary>
    /// Creates a new descriptor for <see cref="ProjectionProvider"/>
    /// </summary>
    /// <param name="context">The descriptor context.</param>
    /// <param name="scope">The scope</param>
    public static ProjectionProviderDescriptor New(
        IDescriptorContext context,
        string? scope) =>
        new(context, scope);
}
