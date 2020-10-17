using System;
using HotChocolate.Data.Utilities;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Sorting
{
    /// <summary>
    /// The sort convention extensions can be used to extend a sort convention.
    /// </summary>
    public class SortConventionExtension
        : ConventionExtension<SortConventionDefinition>
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

        protected override SortConventionDefinition CreateDefinition(
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

            return descriptor.CreateDefinition();
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
            if (convention is SortConvention sortConvention &&
                Definition is {} &&
                sortConvention.Definition is {})
            {
                ExtensionHelpers.MergeDictionary(
                    Definition.Bindings,
                    sortConvention.Definition.Bindings);

                ExtensionHelpers.MergeListDictionary(
                    Definition.Configurations,
                    sortConvention.Definition.Configurations);

                ExtensionHelpers.MergeListDictionary(
                    Definition.EnumConfigurations,
                    sortConvention.Definition.EnumConfigurations);

                sortConvention.Definition.Operations.AddRange(Definition.Operations);

                sortConvention.Definition.ProviderExtensions.AddRange(
                    Definition.ProviderExtensions);

                sortConvention.Definition.ProviderExtensionsTypes.AddRange(
                    Definition.ProviderExtensionsTypes);

                if (Definition.ArgumentName != SortConventionDefinition.DefaultArgumentName)
                {
                    sortConvention.Definition.ArgumentName = Definition.ArgumentName;
                }

                if (Definition.Provider is {})
                {
                    sortConvention.Definition.Provider = Definition.Provider;
                }

                if (Definition.ProviderInstance is {})
                {
                    sortConvention.Definition.ProviderInstance = Definition.ProviderInstance;
                }

                if (Definition.DefaultBinding is {})
                {
                    sortConvention.Definition.DefaultBinding = Definition.DefaultBinding;
                }
            }
        }
    }
}
