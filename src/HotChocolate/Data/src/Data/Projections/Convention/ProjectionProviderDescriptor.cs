using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Projections;

public class ProjectionProviderDescriptor
    : IProjectionProviderDescriptor
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
    public IProjectionProviderDescriptor RegisterFieldHandler<THandler>()
        where THandler : IProjectionFieldHandler
    {
        Configuration.Handlers.Add((typeof(THandler), null));
        return this;
    }

    /// <inheritdoc />
    public IProjectionProviderDescriptor RegisterFieldHandler<THandler>(THandler handler)
        where THandler : IProjectionFieldHandler
    {
        Configuration.Handlers.Add((typeof(THandler), handler));
        return this;
    }

    /// <inheritdoc />
    public IProjectionProviderDescriptor RegisterFieldInterceptor<THandler>()
        where THandler : IProjectionFieldInterceptor
    {
        Configuration.Interceptors.Add((typeof(THandler), null));
        return this;
    }

    /// <inheritdoc />
    public IProjectionProviderDescriptor RegisterFieldInterceptor<THandler>(THandler handler)
        where THandler : IProjectionFieldInterceptor
    {
        Configuration.Interceptors.Add((typeof(THandler), handler));
        return this;
    }

    /// <inheritdoc />
    public IProjectionProviderDescriptor RegisterOptimizer<THandler>()
        where THandler : IProjectionOptimizer
    {
        Configuration.Optimizers.Add((typeof(THandler), null));
        return this;
    }

    /// <inheritdoc />
    public IProjectionProviderDescriptor RegisterOptimizer<THandler>(THandler handler)
        where THandler : IProjectionOptimizer
    {
        Configuration.Optimizers.Add((typeof(THandler), handler));
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
        new ProjectionProviderDescriptor(context, scope);
}
