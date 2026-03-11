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
        Configuration.FieldHandlerConfigurations.Add(new ProjectionFieldHandlerConfiguration(ctx => factory(ctx)));
        return this;
    }

    /// <inheritdoc />
    public IProjectionProviderDescriptor RegisterFieldHandler<THandler>(THandler handler)
        where THandler : IProjectionFieldHandler
    {
        Configuration.FieldHandlerConfigurations.Add(new ProjectionFieldHandlerConfiguration(handler));
        return this;
    }

    /// <inheritdoc />
    public IProjectionProviderDescriptor RegisterFieldInterceptor<THandler>(
        Func<ProjectionProviderContext, THandler> factory)
        where THandler : IProjectionFieldInterceptor
    {
        Configuration.FieldInterceptorConfigurations.Add(new ProjectionFieldInterceptorConfiguration(ctx => factory(ctx)));
        return this;
    }

    /// <inheritdoc />
    public IProjectionProviderDescriptor RegisterFieldInterceptor<THandler>(THandler handler)
        where THandler : IProjectionFieldInterceptor
    {
        Configuration.FieldInterceptorConfigurations.Add(new ProjectionFieldInterceptorConfiguration(handler));
        return this;
    }

    /// <inheritdoc />
    public IProjectionProviderDescriptor RegisterOptimizer<THandler>(
        Func<ProjectionProviderContext, THandler> factory)
        where THandler : IProjectionOptimizer
    {
        Configuration.OptimizerConfigurations.Add(new ProjectionOptimizerConfiguration(ctx => factory(ctx)));
        return this;
    }

    /// <inheritdoc />
    public IProjectionProviderDescriptor RegisterOptimizer<THandler>(THandler handler)
        where THandler : IProjectionOptimizer
    {
        Configuration.OptimizerConfigurations.Add(new ProjectionOptimizerConfiguration(handler));
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
