using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Projections;

public class ProjectionConventionExtension
    : ConventionExtension<ProjectionConventionConfiguration>
{
    private Action<IProjectionConventionDescriptor>? _configure;

    protected ProjectionConventionExtension()
    {
        _configure = Configure;
    }

    public ProjectionConventionExtension(Action<IProjectionConventionDescriptor> configure)
    {
        _configure = configure ??
            throw new ArgumentNullException(nameof(configure));
    }

    protected override ProjectionConventionConfiguration CreateConfiguration(
        IConventionContext context)
    {
        if (_configure is null)
        {
            throw new InvalidOperationException(
                DataResources.ProjectionConvention_NoConfigurationSpecified);
        }

        var descriptor = ProjectionConventionDescriptor.New(
            context.DescriptorContext,
            context.Scope);

        _configure(descriptor);
        _configure = null;

        return descriptor.CreateConfiguration();
    }

    protected internal new void Initialize(IConventionContext context)
    {
        base.Initialize(context);
    }

    protected virtual void Configure(IProjectionConventionDescriptor descriptor)
    {
    }

    public override void Merge(IConventionContext context, Convention convention)
    {
        if (convention is ProjectionConvention projectionConvention
            && Configuration is not null
            && projectionConvention.Configuration is not null)
        {
            projectionConvention.Configuration.ProviderExtensions.AddRange(
                Configuration.ProviderExtensions);

            projectionConvention.Configuration.ProviderExtensionsTypes.AddRange(
                Configuration.ProviderExtensionsTypes);

            if (Configuration.Provider is not null)
            {
                projectionConvention.Configuration.Provider = Configuration.Provider;
            }

            if (Configuration.ProviderInstance is not null)
            {
                projectionConvention.Configuration.ProviderInstance = Configuration.ProviderInstance;
            }
        }
    }
}
