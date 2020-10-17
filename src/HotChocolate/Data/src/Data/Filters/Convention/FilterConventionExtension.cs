using System;
using HotChocolate.Data.Utilities;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters
{
    /// <summary>
    /// The filter convention extension can be used to extend a convention.
    /// </summary>
    public class FilterConventionExtension
        : ConventionExtension<FilterConventionDefinition>
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

        protected override FilterConventionDefinition CreateDefinition(
            IConventionContext context)
        {
            if (_configure is null)
            {
                throw new InvalidOperationException(DataResources.FilterConvention_NoConfigurationSpecified);
            }

            var descriptor = FilterConventionDescriptor.New(
                context.DescriptorContext,
                context.Scope);

            _configure(descriptor);
            _configure = null;

            return descriptor.CreateDefinition();
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
            if (convention is FilterConvention filterConvention &&
                Definition is not null &&
                filterConvention.Definition is not null)
            {
                ExtensionHelpers.MergeDictionary(
                    Definition.Bindings,
                    filterConvention.Definition.Bindings);

                ExtensionHelpers.MergeListDictionary(
                    Definition.Configurations,
                    filterConvention.Definition.Configurations);

                filterConvention.Definition.Operations.AddRange(Definition.Operations);

                filterConvention.Definition.ProviderExtensions.AddRange(
                    Definition.ProviderExtensions);

                filterConvention.Definition.ProviderExtensionsTypes.AddRange(
                    Definition.ProviderExtensionsTypes);

                if (Definition.ArgumentName != FilterConventionDefinition.DefaultArgumentName)
                {
                    filterConvention.Definition.ArgumentName = Definition.ArgumentName;
                }

                if (Definition.Provider is not null)
                {
                    filterConvention.Definition.Provider = Definition.Provider;
                }

                if (Definition.ProviderInstance is not null)
                {
                    filterConvention.Definition.ProviderInstance = Definition.ProviderInstance;
                }
            }
        }
    }
}
