using System;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Data.DataResources;

namespace HotChocolate.Data.Sorting
{
    public class SortConventionDescriptor
        : ISortConventionDescriptor
    {
        private readonly Dictionary<int, SortOperationConventionDescriptor> _operations =
            new Dictionary<int, SortOperationConventionDescriptor>();

        protected SortConventionDescriptor(IDescriptorContext context, string? scope)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Definition.Scope = scope;
        }

        protected IDescriptorContext Context { get; }

        protected SortConventionDefinition Definition { get; } =
            new SortConventionDefinition();

        public SortConventionDefinition CreateDefinition()
        {
            // collect all operation configurations and add them to the convention definition.
            foreach (SortOperationConventionDescriptor operation in _operations.Values)
            {
                Definition.Operations.Add(operation.CreateDefinition());
            }

            return Definition;
        }

        /// <inheritdoc />
        public ISortOperationConventionDescriptor Operation(int operationId)
        {
            if (_operations.TryGetValue(
                operationId,
                out SortOperationConventionDescriptor? descriptor))
            {
                return descriptor;
            }

            descriptor = SortOperationConventionDescriptor.New(operationId);
            _operations.Add(operationId, descriptor);
            return descriptor;
        }

        /// <inheritdoc />
        public ISortConventionDescriptor DefaultBinding<TSortType>()
        {
            Definition.DefaultBinding = typeof(TSortType);
            return this;
        }

        /// <inheritdoc />
        public ISortConventionDescriptor BindRuntimeType<TRuntimeType, TSortType>() =>
            BindRuntimeType(typeof(TRuntimeType), typeof(TSortType));

        /// <inheritdoc />
        public ISortConventionDescriptor BindRuntimeType(Type runtimeType, Type sortType)
        {
            if (runtimeType is null)
            {
                throw new ArgumentNullException(nameof(runtimeType));
            }

            if (sortType is null)
            {
                throw new ArgumentNullException(nameof(sortType));
            }

            if (!typeof(SortInputType).IsAssignableFrom(sortType) &&
                !typeof(SortEnumType).IsAssignableFrom(sortType))
            {
                throw new ArgumentException(
                    SortConventionDescriptor_MustInheritFromSortInputOrEnumType,
                    nameof(sortType));
            }

            Definition.Bindings.Add(runtimeType, sortType);
            return this;
        }

        /// <inheritdoc />
        public ISortConventionDescriptor Configure<TSortType>(
            ConfigureSortInputType configure)
            where TSortType : SortInputType =>
            Configure(
                Context.TypeInspector.GetTypeRef(
                    typeof(TSortType),
                    TypeContext.Input,
                    Definition.Scope),
                configure);

        /// <inheritdoc />
        public ISortConventionDescriptor Configure<TSortType, TRuntimeType>(
            ConfigureSortInputType<TRuntimeType> configure)
            where TSortType : SortInputType<TRuntimeType> =>
            Configure(
                Context.TypeInspector.GetTypeRef(
                    typeof(TSortType),
                    TypeContext.Input,
                    Definition.Scope),
                d =>
                {
                    configure.Invoke(
                        SortInputTypeDescriptor.From<TRuntimeType>(
                            (SortInputTypeDescriptor)d,
                            Definition.Scope));
                });

        /// <inheritdoc />
        public ISortConventionDescriptor ConfigureEnum<TSortEnumType>(
            ConfigureSortEnumType configure)
            where TSortEnumType : SortEnumType
        {
            ExtendedTypeReference typeReference =
                Context.TypeInspector.GetTypeRef(
                    typeof(TSortEnumType),
                    TypeContext.None,
                    Definition.Scope);

            if (!Definition.EnumConfigurations.TryGetValue(
                typeReference,
                out List<ConfigureSortEnumType>? configurations))
            {
                configurations = new List<ConfigureSortEnumType>();
                Definition.EnumConfigurations.Add(typeReference, configurations);
            }

            configurations.Add(configure);
            return this;
        }

        protected ISortConventionDescriptor Configure(
            ITypeReference typeReference,
            ConfigureSortInputType configure)
        {
            if (!Definition.Configurations.TryGetValue(
                typeReference,
                out List<ConfigureSortInputType>? configurations))
            {
                configurations = new List<ConfigureSortInputType>();
                Definition.Configurations.Add(typeReference, configurations);
            }

            configurations.Add(configure);
            return this;
        }

        /// <inheritdoc />
        public ISortConventionDescriptor Provider<TProvider>()
            where TProvider : class, ISortProvider =>
            Provider(typeof(TProvider));

        /// <inheritdoc />
        public ISortConventionDescriptor Provider<TProvider>(TProvider provider)
            where TProvider : class, ISortProvider
        {
            Definition.Provider = typeof(TProvider);
            Definition.ProviderInstance = provider;
            return this;
        }

        /// <inheritdoc />
        public ISortConventionDescriptor Provider(Type provider)
        {
            if (provider is null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (!typeof(ISortProvider).IsAssignableFrom(provider))
            {
                throw new ArgumentException(
                    SortConventionDescriptor_MustImplementISortProvider,
                    nameof(provider));
            }

            Definition.Provider = provider;
            return this;
        }

        /// <inheritdoc />
        public ISortConventionDescriptor ArgumentName(NameString argumentName)
        {
            Definition.ArgumentName = argumentName;
            return this;
        }

        public ISortConventionDescriptor AddProviderExtension<TExtension>() where TExtension : class, ISortProviderExtension
        {
            Definition.ProviderExtensionsTypes.Add(typeof(TExtension));
            return this;
        }

        public ISortConventionDescriptor AddProviderExtension<TExtension>(TExtension provider) where TExtension : class, ISortProviderExtension
        {
            Definition.ProviderExtensions.Add(provider);
            return this;
        }

        /// <summary>
        /// Creates a new descriptor for <see cref="SortConvention"/>
        /// </summary>
        /// <param name="context">The descriptor context.</param>
        /// <param name="scope">The scope</param>
        public static SortConventionDescriptor New(IDescriptorContext context, string? scope) =>
            new SortConventionDescriptor(context, scope);
    }
}
