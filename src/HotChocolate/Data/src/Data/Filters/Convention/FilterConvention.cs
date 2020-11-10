using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using static HotChocolate.Data.DataResources;
using static HotChocolate.Data.ThrowHelper;

namespace HotChocolate.Data.Filters
{
    /// <summary>
    /// The filter convention provides defaults for inferring filters.
    /// </summary>
    public class FilterConvention
        : Convention<FilterConventionDefinition>
        , IFilterConvention
    {
        private const string _typePostFix = "FilterInput";

        private Action<IFilterConventionDescriptor>? _configure;
        private INamingConventions _namingConventions = default!;
        private IReadOnlyDictionary<int, FilterOperation> _operations = default!;
        private IDictionary<Type, Type> _bindings = default!;
        private IDictionary<ITypeReference, List<ConfigureFilterInputType>> _configs = default!;

        private NameString _argumentName;
        private IFilterProvider _provider = default!;
        private ITypeInspector _typeInspector = default!;

        protected FilterConvention()
        {
            _configure = Configure;
        }

        public FilterConvention(Action<IFilterConventionDescriptor> configure)
        {
            _configure = configure ??
                throw new ArgumentNullException(nameof(configure));
        }

        internal new FilterConventionDefinition? Definition => base.Definition;

        protected override FilterConventionDefinition CreateDefinition(
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

            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IFilterConventionDescriptor descriptor)
        {
        }

        protected override void Complete(IConventionContext context)
        {
            if (Definition?.Provider is null)
            {
                throw FilterConvention_NoProviderFound(GetType(), Definition?.Scope);
            }

            if (Definition.ProviderInstance is null)
            {
                _provider =
                    context.Services.GetOrCreateService<IFilterProvider>(Definition.Provider) ??
                    throw FilterConvention_NoProviderFound(GetType(), Definition.Scope);
            }
            else
            {
                _provider = Definition.ProviderInstance;
            }

            _namingConventions = context.DescriptorContext.Naming;
            _operations = Definition.Operations.ToDictionary(
                x => x.Id,
                FilterOperation.FromDefinition);
            _bindings = Definition.Bindings;
            _configs = Definition.Configurations;
            _argumentName = Definition.ArgumentName;

            if (_provider is IFilterProviderConvention init)
            {
                IReadOnlyList<IFilterProviderExtension> extensions =
                    CollectExtensions(context.Services, Definition);
                init.Initialize(context, this);
                MergeExtensions(context, init, extensions);
                init.Complete(context);
            }

            _typeInspector = context.DescriptorContext.TypeInspector;

            // It is important to always call base to continue the cleanup and the disposal of the
            // definition
            base.Complete(context);
        }


        /// <inheritdoc />
        public virtual NameString GetTypeName(Type runtimeType)
        {
            if (runtimeType is null)
            {
                throw new ArgumentNullException(nameof(runtimeType));
            }

            string name = _namingConventions.GetTypeName(runtimeType);

            if (!name.EndsWith(_typePostFix, StringComparison.Ordinal))
            {
                name += _typePostFix;
            }

            return name;
        }

        /// <inheritdoc />
        public virtual string? GetTypeDescription(Type runtimeType) =>
            _namingConventions.GetTypeDescription(runtimeType, TypeKind.InputObject);

        /// <inheritdoc />
        public virtual NameString GetFieldName(MemberInfo member) =>
            _namingConventions.GetMemberName(member, MemberKind.InputObjectField);

        /// <inheritdoc />
        public virtual string? GetFieldDescription(MemberInfo member) =>
            _namingConventions.GetMemberDescription(member, MemberKind.InputObjectField);

        /// <inheritdoc />
        public virtual ExtendedTypeReference GetFieldType(MemberInfo member)
        {
            if (member is null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (TryCreateFilterType(_typeInspector.GetReturnType(member, true), out Type? rt))
            {
                return _typeInspector.GetTypeRef(rt, TypeContext.Input, Scope);
            }

            throw FilterConvention_TypeOfMemberIsUnknown(member);
        }

        /// <inheritdoc />
        public NameString GetOperationName(int operation)
        {
            if (_operations.TryGetValue(operation, out FilterOperation? operationConvention))
            {
                return operationConvention.Name;
            }

            throw FilterConvention_OperationNameNotFound(operation);
        }

        /// <inheritdoc />
        public string? GetOperationDescription(int operationId)
        {
            if (_operations.TryGetValue(operationId, out FilterOperation? operationConvention))
            {
                return operationConvention.Description;
            }

            return null;
        }

        /// <inheritdoc />
        public NameString GetArgumentName() => _argumentName;

        /// <inheritdoc cref="IFilterConvention"/>
        public void ApplyConfigurations(
            ITypeReference typeReference,
            IFilterInputTypeDescriptor descriptor)
        {
            if (_configs.TryGetValue(
                typeReference,
                out List<ConfigureFilterInputType>? configurations))
            {
                foreach (ConfigureFilterInputType configure in configurations)
                {
                    configure(descriptor);
                }

                if (descriptor is FilterInputTypeDescriptor inputTypeDescriptor)
                {
                    inputTypeDescriptor.CreateDefinition();
                }
            }
        }

        public FieldMiddleware CreateExecutor<TEntityType>() =>
            _provider.CreateExecutor<TEntityType>(_argumentName);

        public virtual void ConfigureField(IObjectFieldDescriptor descriptor) =>
            _provider.ConfigureField(_argumentName, descriptor);

        public bool TryGetHandler(
            ITypeDiscoveryContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition,
            [NotNullWhen(true)] out IFilterFieldHandler? handler)
        {
            foreach (IFilterFieldHandler filterFieldHandler in _provider.FieldHandlers)
            {
                if (filterFieldHandler.CanHandle(context, typeDefinition, fieldDefinition))
                {
                    handler = filterFieldHandler;
                    return true;
                }
            }

            handler = null;
            return false;
        }

        private bool TryCreateFilterType(
            IExtendedType runtimeType,
            [NotNullWhen(true)] out Type? type)
        {
            if (_bindings.TryGetValue(runtimeType.Source, out type))
            {
                return true;
            }

            if (runtimeType.IsArrayOrList)
            {
                if (runtimeType.ElementType is {} &&
                    TryCreateFilterType(runtimeType.ElementType, out Type? elementType))
                {
                    type = typeof(ListFilterInput<>).MakeGenericType(elementType);
                    return true;
                }
            }

            if (runtimeType.Type.IsEnum)
            {
                type = typeof(EnumOperationFilterInput<>).MakeGenericType(runtimeType.Source);
                return true;
            }

            if (runtimeType.Type.IsClass)
            {
                type = typeof(FilterInputType<>).MakeGenericType(runtimeType.Source);
                return true;
            }

            type = null;
            return false;
        }

        private static IReadOnlyList<IFilterProviderExtension> CollectExtensions(
            IServiceProvider serviceProvider,
            FilterConventionDefinition definition)
        {
            List<IFilterProviderExtension> extensions = new List<IFilterProviderExtension>();
            extensions.AddRange(definition.ProviderExtensions);
            foreach (var extensionType in definition.ProviderExtensionsTypes)
            {
                if (serviceProvider.TryGetOrCreateService<IFilterProviderExtension>(
                    extensionType,
                    out var createdExtension))
                {
                    extensions.Add(createdExtension);
                }
            }

            return extensions;
        }

        private void MergeExtensions(
            IConventionContext context,
            IFilterProviderConvention provider,
            IReadOnlyList<IFilterProviderExtension> extensions)
        {
            if (provider is Convention providerConvention)
            {
                for (var m = 0; m < extensions.Count; m++)
                {
                    if (extensions[m] is IFilterProviderConvention extensionConvention)
                    {
                        extensionConvention.Initialize(context, this);
                        extensions[m].Merge(context, providerConvention);
                        extensionConvention.Complete(context);
                    }
                }
            }
        }
    }
}
