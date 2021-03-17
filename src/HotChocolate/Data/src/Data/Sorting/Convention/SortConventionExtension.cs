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
                Definition is not null &&
                sortConvention.Definition is not null)
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

                for (var i = 0; i < Definition.Operations.Count; i++)
                {
                    sortConvention.Definition.Operations.Add(Definition.Operations[i]);
                }

                for (var i = 0; i < Definition.ProviderExtensions.Count; i++)
                {
                    sortConvention.Definition.ProviderExtensions.Add(
                        Definition.ProviderExtensions[i]);
                }

                for (var i = 0; i < Definition.ProviderExtensionsTypes.Count; i++)
                {
                    sortConvention.Definition.ProviderExtensionsTypes.Add(
                        Definition.ProviderExtensionsTypes[i]);
                }

                if (Definition.ArgumentName != SortConventionDefinition.DefaultArgumentName)
                {
                    sortConvention.Definition.ArgumentName = Definition.ArgumentName;
                }

                if (Definition.Provider is not null)
                {
                    sortConvention.Definition.Provider = Definition.Provider;
                }

                if (Definition.ProviderInstance is not null)
                {
                    sortConvention.Definition.ProviderInstance = Definition.ProviderInstance;
                }

                if (Definition.DefaultBinding is not null)
                {
                    sortConvention.Definition.DefaultBinding = Definition.DefaultBinding;
                }
            }
        }
    }
}
