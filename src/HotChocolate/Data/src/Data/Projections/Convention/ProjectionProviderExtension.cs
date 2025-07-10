using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Projections;

public class ProjectionProviderExtension
    : ConventionExtension<ProjectionProviderConfiguration>
    , IProjectionProviderExtension
    , IProjectionProviderConvention
{
    private Action<IProjectionProviderDescriptor>? _configure;

    protected ProjectionProviderExtension()
    {
        _configure = Configure;
    }

    public ProjectionProviderExtension(Action<IProjectionProviderDescriptor> configure)
    {
        _configure = configure ??
            throw new ArgumentNullException(nameof(configure));
    }

    void IProjectionProviderConvention.Initialize(IConventionContext context)
    {
        Initialize(context);
    }

    void IProjectionProviderConvention.Complete(IConventionContext context)
    {
        Complete(context);
    }

    protected override ProjectionProviderConfiguration CreateConfiguration(IConventionContext context)
    {
        if (_configure is null)
        {
            throw new InvalidOperationException(
                DataResources.ProjectionProvider_NoConfigurationSpecified);
        }

        var descriptor = ProjectionProviderDescriptor.New(
            context.DescriptorContext,
            context.Scope);

        _configure(descriptor);
        _configure = null;

        return descriptor.CreateConfiguration();
    }

    protected virtual void Configure(IProjectionProviderDescriptor descriptor) { }

    public override void Merge(IConventionContext context, Convention convention)
    {
        if (Configuration is not null &&
            convention is ProjectionProvider projectionProvider &&
            projectionProvider.Configuration is { } target)
        {
            // Provider extensions should be applied by default before the default handlers, as
            // the interceptor picks up the first handler. A provider extension should adds more
            // specific handlers than the default providers
            for (var i = Configuration.Handlers.Count - 1; i >= 0; i--)
            {
                target.Handlers.Insert(0, Configuration.Handlers[i]);
            }
        }
    }
}
