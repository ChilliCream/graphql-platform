using HotChocolate.Types.Descriptors;
using static HotChocolate.Data.DataResources;

namespace HotChocolate.Data.Sorting;

public abstract class SortProviderExtensions<TContext>
    : ConventionExtension<SortProviderConfiguration<TContext>>,
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
        Initialize(context);
    }

    void ISortProviderConvention.Complete(IConventionContext context)
    {
        Complete(context);
    }

    protected override SortProviderConfiguration<TContext> CreateConfiguration(IConventionContext context)
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
        for (var i = Configuration.HandlerFactories.Count - 1; i >= 0; i--)
        {
            target.HandlerFactories.Insert(0, Configuration.HandlerFactories[i]);
        }

        for (var i = Configuration.OperationHandlerFactories.Count - 1; i >= 0; i--)
        {
            target.OperationHandlerFactories.Insert(0, Configuration.OperationHandlerFactories[i]);
        }
    }
}
