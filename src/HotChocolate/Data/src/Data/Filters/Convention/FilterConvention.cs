using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using static HotChocolate.Data.DataResources;
using static HotChocolate.Data.ThrowHelper;

namespace HotChocolate.Data.Filters
{
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

        protected FilterConvention()
        {
            _configure = Configure;
        }

        public FilterConvention(Action<IFilterConventionDescriptor> configure)
        {
            _configure = configure ??
                throw new ArgumentNullException(nameof(configure));
        }

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

        [SuppressMessage("HotChocolate", "CA1062")]
        protected override void OnComplete(
            IConventionContext context,
            FilterConventionDefinition definition)
        {
            if (definition.Provider is null)
            {
                throw FilterConvention_NoProviderFound(GetType(), definition.Scope);
            }

            if (definition.ProviderInstance is null)
            {
                _provider =
                    context.Services.GetOrCreateService<IFilterProvider>(definition.Provider) ??
                    throw FilterConvention_NoProviderFound(GetType(), definition.Scope);
            }
            else
            {
                _provider = definition.ProviderInstance;
            }

            _namingConventions = context.DescriptorContext.Naming;
            _operations = definition.Operations.ToDictionary(
                x => x.Id,
                FilterOperation.FromDefinition);
            _bindings = definition.Bindings;
            _configs = definition.Configurations;
            _argumentName = definition.ArgumentName;

            /*
            if (_provider is FilterProviderBase init)
            {
                IFilterProviderInitializationContext providerContext =
                    FilterProviderInitializationContext.From(context, this);
                _provider.In(providerContext);
            }
            */
        }

        /// <inheritdoc cref="IFilterConvention"/>
        public virtual NameString GetTypeName(Type runtimeType) =>
            _namingConventions.GetTypeName(runtimeType, TypeKind.Object) + _typePostFix;

        /// <inheritdoc cref="IFilterConvention"/>
        public virtual string? GetTypeDescription(Type runtimeType) =>
            _namingConventions.GetTypeDescription(runtimeType, TypeKind.InputObject);

        /// <inheritdoc cref="IFilterConvention"/>
        public virtual NameString GetFieldName(MemberInfo member) =>
            _namingConventions.GetMemberName(member, MemberKind.InputObjectField);

        /// <inheritdoc cref="IFilterConvention"/>
        public virtual string? GetFieldDescription(MemberInfo member) =>
            _namingConventions.GetMemberDescription(member, MemberKind.InputObjectField);

        /// <inheritdoc cref="IFilterConvention"/>
        public virtual ClrTypeReference GetFieldType(MemberInfo member)
        {
            if (member is null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (TryGetTypeOfMember(member, out Type? returnType))
            {
                return TypeReference.Create(returnType, TypeContext.Input, Scope);
            }

            throw FilterConvention_TypeOfMemberIsUnknown(member);
        }

        /// <inheritdoc cref="IFilterConvention"/>
        public NameString GetOperationName(int operation)
        {
            if (_operations.TryGetValue(operation, out FilterOperation? operationConvention))
            {
                return operationConvention.Name;
            }

            throw FilterConvention_OperationNameNotFound(operation);
        }

        /// <inheritdoc cref="IFilterConvention"/>
        public string? GetOperationDescription(int operationId)
        {
            if (_operations.TryGetValue(operationId, out FilterOperation? operationConvention))
            {
                return operationConvention.Description;
            }

            return null;
        }

        /// <inheritdoc cref="IFilterConvention"/>
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
            }
        }

        public IFilterExecutor<TEntityType> CreateExecutor<TEntityType>() =>
            _provider.CreateExecutor<TEntityType>(_argumentName);

        public bool TryGetHandler(
            ITypeDiscoveryContext context,
            FilterInputTypeDefinition typeDefinition,
            FilterFieldDefinition fieldDefinition,
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

        private bool TryGetTypeOfMember(
            MemberInfo member,
            [NotNullWhen(true)] out Type? type)
        {
            switch (member)
            {
                case PropertyInfo p when TryGetTypeOfRuntimeType(p.PropertyType, out type):
                case MethodInfo m when TryGetTypeOfRuntimeType(m.ReturnType, out type):
                    return true;

                default:
                    type = null;
                    return false;
            }
        }

        private bool TryGetTypeOfRuntimeType(
            Type runtimeType,
            [NotNullWhen(true)] out Type? type)
        {
            Type underlyingType = runtimeType;
            if (runtimeType.IsGenericType &&
                System.Nullable.GetUnderlyingType(runtimeType) is { } innerNullableType)
            {
                underlyingType = innerNullableType;
            }

            if (_bindings.TryGetValue(runtimeType, out type))
            {
                return true;
            }

            if (DotNetTypeInfoFactory.IsListType(underlyingType))
            {
                if (!TypeInspector.Default.TryCreate(
                    underlyingType,
                    out Utilities.TypeInfo typeInfo))
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            FilterConvention_UnknownType,
                            underlyingType.FullName ?? underlyingType.Name),
                        nameof(runtimeType));
                }

                if (TryGetTypeOfRuntimeType(typeInfo.ClrType, out Type? clrType))
                {
                    type = typeof(ListFilterInput<>).MakeGenericType(clrType);
                    return true;
                }
            }

            if (underlyingType.IsEnum)
            {
                type = typeof(EnumOperationInput<>).MakeGenericType(runtimeType);
                return true;
            }

            if (underlyingType.IsClass)
            {
                type = typeof(FilterInputType<>).MakeGenericType(runtimeType);
                return true;
            }

            type = null;
            return false;
        }
    }
}
