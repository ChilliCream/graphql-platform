using System;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Data.DataResources;

namespace HotChocolate.Data.Filters
{
    public class FilterConventionDescriptor
        : IFilterConventionDescriptor
    {
        private readonly Dictionary<int, FilterOperationConventionDescriptor> _operations =
            new Dictionary<int, FilterOperationConventionDescriptor>();

        protected FilterConventionDescriptor(IDescriptorContext context, string? scope)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Definition.Scope = scope;
        }

        protected IDescriptorContext Context { get; }

        protected FilterConventionDefinition Definition { get; } =
            new FilterConventionDefinition();

        public FilterConventionDefinition CreateDefinition()
        {
            // collect all operation configurations and add them to the convention definition.
            foreach (FilterOperationConventionDescriptor operation in _operations.Values)
            {
                Definition.Operations.Add(operation.CreateDefinition());
            }

            return Definition;
        }

        /// <inheritdoc />
        public IFilterOperationConventionDescriptor Operation(int operationId)
        {
            if (_operations.TryGetValue(
                operationId,
                out FilterOperationConventionDescriptor? descriptor))
            {
                return descriptor;
            }

            descriptor = FilterOperationConventionDescriptor.New(operationId);
            _operations.Add(operationId, descriptor);
            return descriptor;
        }

        /// <inheritdoc />
        public IFilterConventionDescriptor BindRuntimeType<TRuntimeType, TFilterType>()
            where TFilterType : FilterInputType =>
            BindRuntimeType(typeof(TRuntimeType), typeof(TFilterType));

        /// <inheritdoc />
        public IFilterConventionDescriptor BindRuntimeType(Type runtimeType, Type filterType)
        {
            if (runtimeType is null)
            {
                throw new ArgumentNullException(nameof(runtimeType));
            }

            if (filterType is null)
            {
                throw new ArgumentNullException(nameof(filterType));
            }

            if (!typeof(FilterInputType).IsAssignableFrom(filterType))
            {
                throw new ArgumentException(
                    FilterConventionDescriptor_MustInheritFromFilterInputType,
                    nameof(filterType));
            }

            Definition.Bindings[runtimeType] = filterType;
            return this;
        }

        /// <inheritdoc />
        public IFilterConventionDescriptor Configure<TFilterType>(
            ConfigureFilterInputType configure)
            where TFilterType : FilterInputType =>
            Configure(
                Context.TypeInspector.GetTypeRef(
                    typeof(TFilterType),
                    TypeContext.Input,
                    Definition.Scope),
                configure);

        /// <inheritdoc />
        public IFilterConventionDescriptor Configure<TFilterType, TRuntimeType>(
            ConfigureFilterInputType<TRuntimeType> configure)
            where TFilterType : FilterInputType<TRuntimeType> =>
            Configure(
                Context.TypeInspector.GetTypeRef(
                    typeof(TFilterType),
                    TypeContext.Input,
                    Definition.Scope),
                d =>
                {
                    configure.Invoke(
                        FilterInputTypeDescriptor.From<TRuntimeType>(
                            (FilterInputTypeDescriptor)d,
                            Definition.Scope));
                });

        protected IFilterConventionDescriptor Configure(
            ITypeReference typeReference,
            ConfigureFilterInputType configure)
        {
            if (!Definition.Configurations.TryGetValue(
                typeReference,
                out List<ConfigureFilterInputType>? configurations))
            {
                configurations = new List<ConfigureFilterInputType>();
                Definition.Configurations.Add(typeReference, configurations);
            }

            configurations.Add(configure);
            return this;
        }

        /// <inheritdoc />
        public IFilterConventionDescriptor Provider<TProvider>()
            where TProvider : class, IFilterProvider =>
            Provider(typeof(TProvider));

        /// <inheritdoc />
        public IFilterConventionDescriptor Provider<TProvider>(TProvider provider)
            where TProvider : class, IFilterProvider
        {
            Definition.Provider = typeof(TProvider);
            Definition.ProviderInstance = provider;
            return this;
        }

        /// <inheritdoc />
        public IFilterConventionDescriptor Provider(Type provider)
        {
            if (provider is null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (!typeof(IFilterProvider).IsAssignableFrom(provider))
            {
                throw new ArgumentException(
                    FilterConventionDescriptor_MustImplementIFilterProvider,
                    nameof(provider));
            }

            Definition.Provider = provider;
            return this;
        }

        /// <inheritdoc />
        public IFilterConventionDescriptor ArgumentName(NameString argumentName)
        {
            Definition.ArgumentName = argumentName;
            return this;
        }

        public IFilterConventionDescriptor AddProviderExtension<TExtension>()
            where TExtension : class, IFilterProviderExtension
        {
            Definition.ProviderExtensionsTypes.Add(typeof(TExtension));
            return this;
        }

        public IFilterConventionDescriptor AddProviderExtension<TExtension>(TExtension provider)
            where TExtension : class, IFilterProviderExtension
        {
            Definition.ProviderExtensions.Add(provider);
            return this;
        }

        /// <summary>
        /// Creates a new descriptor for <see cref="FilterConvention"/>
        /// </summary>
        /// <param name="context">The descriptor context.</param>
        /// <param name="scope">The scope</param>
        public static FilterConventionDescriptor New(IDescriptorContext context, string? scope) =>
            new FilterConventionDescriptor(context, scope);
    }
}
