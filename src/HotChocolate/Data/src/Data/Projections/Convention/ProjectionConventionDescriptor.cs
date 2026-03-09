using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Projections;

public class ProjectionConventionDescriptor
    : IProjectionConventionDescriptor
{
    protected ProjectionConventionDescriptor(
        IDescriptorContext context,
        string? scope)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Configuration.Scope = scope;
    }

    protected IDescriptorContext Context { get; }

    protected ProjectionConventionConfiguration Configuration { get; } =
        new ProjectionConventionConfiguration();

    public ProjectionConventionConfiguration CreateConfiguration()
    {
        return Configuration;
    }

    /// <inheritdoc />
    public IProjectionConventionDescriptor Provider<TProvider>()
        where TProvider : class, IProjectionProvider =>
        Provider(typeof(TProvider));

    /// <inheritdoc />
    public IProjectionConventionDescriptor Provider<TProvider>(TProvider provider)
        where TProvider : class, IProjectionProvider
    {
        Configuration.Provider = typeof(TProvider);
        Configuration.ProviderInstance = provider;
        return this;
    }

    /// <inheritdoc />
    public IProjectionConventionDescriptor Provider(Type provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        if (!typeof(IProjectionProvider).IsAssignableFrom(provider))
        {
            throw new ArgumentException(
                DataResources.ProjectionConventionDescriptor_MustImplementIProjectionProvider,
                nameof(provider));
        }

        Configuration.Provider = provider;
        return this;
    }

    /// <inheritdoc />
    public IProjectionConventionDescriptor AddProviderExtension<TExtension>()
        where TExtension : class, IProjectionProviderExtension
    {
        Configuration.ProviderExtensionsTypes.Add(typeof(TExtension));
        return this;
    }

    /// <inheritdoc />
    public IProjectionConventionDescriptor AddProviderExtension<TExtension>(TExtension provider)
        where TExtension : class, IProjectionProviderExtension
    {
        Configuration.ProviderExtensions.Add(provider);
        return this;
    }

    /// <summary>
    /// Creates a new descriptor for <see cref="ProjectionConvention"/>
    /// </summary>
    /// <param name="context">The descriptor context.</param>
    /// <param name="scope">The scope</param>
    public static ProjectionConventionDescriptor New(
        IDescriptorContext context,
        string? scope) =>
        new ProjectionConventionDescriptor(context, scope);
}
