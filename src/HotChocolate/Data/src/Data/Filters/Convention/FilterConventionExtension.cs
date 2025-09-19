using HotChocolate.Data.Utilities;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Data.DataResources;

namespace HotChocolate.Data.Filters;

/// <summary>
/// The filter convention extension can be used to extend a convention.
/// </summary>
public class FilterConventionExtension
    : ConventionExtension<FilterConventionConfiguration>
{
    private Action<IFilterConventionDescriptor>? _configure;

    protected FilterConventionExtension()
    {
        _configure = Configure;
    }

    public FilterConventionExtension(Action<IFilterConventionDescriptor> configure)
    {
        _configure = configure ??
            throw new ArgumentNullException(nameof(configure));
    }

    protected override FilterConventionConfiguration CreateConfiguration(
        IConventionContext context)
    {
        if (_configure is null)
        {
            throw new InvalidOperationException(FilterConvention_NoConfigurationSpecified);
        }

        var descriptor = FilterConventionDescriptor.New(
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

    protected virtual void Configure(IFilterConventionDescriptor descriptor)
    {
    }

    public override void Merge(IConventionContext context, Convention convention)
    {
        if (convention is FilterConvention filterConvention
            && Configuration is not null
            && filterConvention.Configuration is not null)
        {
            ExtensionHelpers.MergeDictionary(
                Configuration.Bindings,
                filterConvention.Configuration.Bindings);

            ExtensionHelpers.MergeListDictionary(
                Configuration.Configurations,
                filterConvention.Configuration.Configurations);

            filterConvention.Configuration.Operations.AddRange(Configuration.Operations);

            filterConvention.Configuration.ProviderExtensions.AddRange(
                Configuration.ProviderExtensions);

            filterConvention.Configuration.ProviderExtensionsTypes.AddRange(
                Configuration.ProviderExtensionsTypes);

            if (Configuration.ArgumentName != FilterConventionConfiguration.DefaultArgumentName)
            {
                filterConvention.Configuration.ArgumentName = Configuration.ArgumentName;
            }

            if (Configuration.Provider is not null)
            {
                filterConvention.Configuration.Provider = Configuration.Provider;
            }

            if (Configuration.ProviderInstance is not null)
            {
                filterConvention.Configuration.ProviderInstance = Configuration.ProviderInstance;
            }
        }
    }
}
