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

        public override void Merge(IConventionContext context, Convention convention)
        {
            if (convention is FilterConvention filterConvention &&
                Definition is {} &&
                filterConvention.Definition is {})
            {
                foreach (KeyValuePair<Type, Type> binding in Definition.Bindings)
                {
                    filterConvention.Definition.Bindings[binding.Key] = binding.Value;
                }

                foreach (KeyValuePair<ITypeReference, List<ConfigureFilterInputType>> configuration
                    in Definition.Configurations)
                {
                    if (filterConvention.Definition.Configurations.TryGetValue(
                        configuration.Key,
                        out var configurations))
                    {
                        configurations.AddRange(configuration.Value);
                    }
                    else
                    {
                        filterConvention.Definition.Configurations[configuration.Key] =
                            configuration.Value;
                    }
                }

                foreach (var operation in Definition.Operations)
                {
                    filterConvention.Definition.Operations.Add(operation);
                }

                if (Definition.ArgumentName != FilterConventionDefinition.DefaultArgumentName)
                {
                    filterConvention.Definition.ArgumentName = Definition.ArgumentName;
                }

                if (Definition.Provider is {})
                {
                    filterConvention.Definition.Provider = Definition.Provider;
                }

                if (Definition.ProviderInstance is {})
                {
                    filterConvention.Definition.ProviderInstance = Definition.ProviderInstance;
                }
            }
        }
    }

    /// <summary>
    /// The filter convention provides defaults for inferring filters.
    /// </summary>
    public class FilterConvention
        : Convention<FilterConventionDefinition>,
          IFilterConvention
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

        internal new FilterConventionDefinition? Definition => base.Definition;

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

        public override void OnComplete(IConventionContext context)
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
                init.Initialize(context);
                // TODO Merge
                init.OnComplete(context);
            }

            _typeInspector = context.DescriptorContext.TypeInspector;
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
    }
}
