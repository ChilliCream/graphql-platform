using HotChocolate.Data.Utilities;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Sorting;

/// <summary>
/// The sort convention extensions can be used to extend a sort convention.
/// </summary>
public class SortConventionExtension
    : ConventionExtension<SortConventionConfiguration>
{
    private Action<ISortConventionDescriptor>? _configure;

    protected SortConventionExtension()
    {
        _configure = Configure;
    }

    public SortConventionExtension(Action<ISortConventionDescriptor> configure)
    {
        _configure = configure ??
            throw new ArgumentNullException(nameof(configure));
    }

    protected override SortConventionConfiguration CreateConfiguration(
        IConventionContext context)
    {
        if (_configure is null)
        {
            throw new InvalidOperationException(
                DataResources.SortConvention_NoConfigurationSpecified);
        }

        var descriptor = SortConventionDescriptor.New(
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

    protected virtual void Configure(ISortConventionDescriptor descriptor)
    {
    }

    public override void Merge(IConventionContext context, Convention convention)
    {
        if (convention is SortConvention sortConvention
            && Configuration is not null
            && sortConvention.Configuration is not null)
        {
            ExtensionHelpers.MergeDictionary(
                Configuration.Bindings,
                sortConvention.Configuration.Bindings);

            ExtensionHelpers.MergeListDictionary(
                Configuration.Configurations,
                sortConvention.Configuration.Configurations);

            ExtensionHelpers.MergeListDictionary(
                Configuration.EnumConfigurations,
                sortConvention.Configuration.EnumConfigurations);

            for (var i = 0; i < Configuration.Operations.Count; i++)
            {
                sortConvention.Configuration.Operations.Add(Configuration.Operations[i]);
            }

            for (var i = 0; i < Configuration.ProviderExtensions.Count; i++)
            {
                sortConvention.Configuration.ProviderExtensions.Add(
                    Configuration.ProviderExtensions[i]);
            }

            for (var i = 0; i < Configuration.ProviderExtensionsTypes.Count; i++)
            {
                sortConvention.Configuration.ProviderExtensionsTypes.Add(
                    Configuration.ProviderExtensionsTypes[i]);
            }

            if (Configuration.ArgumentName != SortConventionConfiguration.DefaultArgumentName)
            {
                sortConvention.Configuration.ArgumentName = Configuration.ArgumentName;
            }

            if (Configuration.Provider is not null)
            {
                sortConvention.Configuration.Provider = Configuration.Provider;
            }

            if (Configuration.ProviderInstance is not null)
            {
                sortConvention.Configuration.ProviderInstance = Configuration.ProviderInstance;
            }

            if (Configuration.DefaultBinding is not null)
            {
                sortConvention.Configuration.DefaultBinding = Configuration.DefaultBinding;
            }
        }
    }
}
