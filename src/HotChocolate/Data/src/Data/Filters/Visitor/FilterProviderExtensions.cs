using HotChocolate.Types.Descriptors;
using static HotChocolate.Data.DataResources;

namespace HotChocolate.Data.Filters;

public abstract class FilterProviderExtensions<TContext>
    : ConventionExtension<FilterProviderConfiguration>,
      IFilterProviderExtension,
      IFilterProviderConvention
    where TContext : IFilterVisitorContext
{
    private Action<IFilterProviderDescriptor<TContext>>? _configure;

    protected FilterProviderExtensions()
    {
        _configure = Configure;
    }

    protected FilterProviderExtensions(Action<IFilterProviderDescriptor<TContext>> configure)
    {
        _configure = configure ??
            throw new ArgumentNullException(nameof(configure));
    }

    void IFilterProviderConvention.Initialize(
        IConventionContext context,
        IFilterConvention convention)
    {
        base.Initialize(context);
    }

    void IFilterProviderConvention.Complete(IConventionContext context)
    {
        Complete(context);
    }

    protected override FilterProviderConfiguration CreateConfiguration(IConventionContext context)
    {
        if (_configure is null)
        {
            throw new InvalidOperationException(FilterProvider_NoConfigurationSpecified);
        }

        var descriptor = FilterProviderDescriptor<TContext>.New();

        _configure(descriptor);
        _configure = null;

        return descriptor.CreateConfiguration();
    }

    protected virtual void Configure(IFilterProviderDescriptor<TContext> descriptor) { }

    public override void Merge(IConventionContext context, Convention convention)
    {
        if (Configuration is not null &&
            convention is FilterProvider<TContext> filterProvider &&
            filterProvider.Configuration is { } target)
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
