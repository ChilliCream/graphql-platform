using HotChocolate.Types.Descriptors;
using static HotChocolate.Data.DataResources;

namespace HotChocolate.Data.Sorting;

public abstract class SortProviderExtensions<TContext>
    : ConventionExtension<SortProviderConfiguration>,
      ISortProviderExtension,
      ISortProviderConvention
    where TContext : ISortVisitorContext
{
    private Action<ISortProviderDescriptor<TContext>>? _configure;

    protected SortProviderExtensions()
    {
        _configure = Configure;
    }

    protected SortProviderExtensions(Action<ISortProviderDescriptor<TContext>> configure)
    {
        _configure = configure ??
            throw new ArgumentNullException(nameof(configure));
    }

    void ISortProviderConvention.Initialize(IConventionContext context, ISortConvention convention)
    {
        base.Initialize(context);
    }

    void ISortProviderConvention.Complete(IConventionContext context)
    {
        Complete(context);
    }

    protected override SortProviderConfiguration CreateConfiguration(IConventionContext context)
    {
        if (_configure is null)
        {
            throw new InvalidOperationException(SortProvider_NoConfigurationSpecified);
        }

        var descriptor = SortProviderDescriptor<TContext>.New();

        _configure(descriptor);
        _configure = null;

        return descriptor.CreateConfiguration();
    }

    protected virtual void Configure(ISortProviderDescriptor<TContext> descriptor) { }

    public override void Merge(IConventionContext context, Convention convention)
    {
        if (Configuration is null
            || convention is not SortProvider<TContext> { Configuration: { } target })
        {
            return;
        }

        // Provider extensions should be applied by default before the default handlers, as
        // the interceptor picks up the first handler. A provider extension should add more
        // specific handlers than the default providers
        for (var i = Configuration.Handlers.Count - 1; i >= 0; i--)
        {
            target.Handlers.Insert(0, Configuration.Handlers[i]);
        }

        for (var i = Configuration.OperationHandlers.Count - 1; i >= 0; i--)
        {
            target.OperationHandlers.Insert(0, Configuration.OperationHandlers[i]);
        }
    }
}
