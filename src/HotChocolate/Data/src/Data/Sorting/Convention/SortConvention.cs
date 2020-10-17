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
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using static HotChocolate.Data.DataResources;
using static HotChocolate.Data.ThrowHelper;

namespace HotChocolate.Data.Sorting
{
    /// <summary>
    /// The sort convention provides defaults for inferring sorting fields.
    /// </summary>
    public class SortConvention
        : Convention<SortConventionDefinition>,
          ISortConvention
    {
        private const string _typePostFix = "SortInput";

        private Action<ISortConventionDescriptor>? _configure;
        private INamingConventions _namingConventions = default!;
        private IReadOnlyDictionary<int, SortOperation> _operations = default!;
        private IDictionary<Type, Type> _bindings = default!;

        private IDictionary<ITypeReference, List<ConfigureSortInputType>> _inputTypeConfigs =
            default!;

        private IDictionary<ITypeReference, List<ConfigureSortEnumType>> _enumTypeConfigs =
            default!;

        private NameString _argumentName;
        private ISortProvider _provider = default!;
        private ITypeInspector _typeInspector = default!;
        private Type? _defaultBinding;

        protected SortConvention()
        {
            _configure = Configure;
        }

        public SortConvention(Action<ISortConventionDescriptor> configure)
        {
            _configure = configure ??
                throw new ArgumentNullException(nameof(configure));
        }

        internal new SortConventionDefinition? Definition => base.Definition;

        protected override SortConventionDefinition CreateDefinition(
            IConventionContext context)
        {
            if (_configure is null)
            {
                throw new InvalidOperationException(SortConvention_NoConfigurationSpecified);
            }

            var descriptor = SortConventionDescriptor.New(
                context.DescriptorContext,
                context.Scope);

            _configure(descriptor);
            _configure = null;

            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(ISortConventionDescriptor descriptor)
        {
        }

        protected override void OnComplete(IConventionContext context)
        {
            if (Definition?.Provider is null)
            {
                throw SortConvention_NoProviderFound(GetType(), Definition?.Scope);
            }

            if (Definition.ProviderInstance is null)
            {
                _provider =
                    context.Services.GetOrCreateService<ISortProvider>(Definition.Provider) ??
                    throw SortConvention_NoProviderFound(GetType(), Definition.Scope);
            }
            else
            {
                _provider = Definition.ProviderInstance;
            }

            _namingConventions = context.DescriptorContext.Naming;

            _operations = Definition.Operations.ToDictionary(
                x => x.Id,
                SortOperation.FromDefinition);

            foreach (var operation in _operations.Values)
            {
                if (!operation.Name.HasValue)
                {
                    throw SortConvention_OperationIsNotNamed(this, operation);
                }
            }

            _bindings = Definition.Bindings;
            _defaultBinding = Definition.DefaultBinding;
            _inputTypeConfigs = Definition.Configurations;
            _enumTypeConfigs = Definition.EnumConfigurations;
            _argumentName = Definition.ArgumentName;

            if (_provider is ISortProviderConvention init)
            {
                IReadOnlyList<ISortProviderExtension> extensions =
                    CollectExtensions(context.Services, Definition);
                init.Initialize(context);
                MergeExtensions(context, init, extensions);
                init.OnComplete(context);
            }

            _typeInspector = context.DescriptorContext.TypeInspector;

            // It is important to always call base to continue the cleanup and the disposal of the
            // definition
            base.OnComplete(context);
        }


        /// <inheritdoc />
        public virtual NameString GetTypeName(Type runtimeType) =>
            _namingConventions.GetTypeName(runtimeType, TypeKind.Object) + _typePostFix;

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

            if (TryCreateSortType(
                _typeInspector.GetReturnType(member, true),
                out Type? returnType))
            {
                return _typeInspector.GetTypeRef(returnType, TypeContext.Input, Scope);
            }

            throw SortConvention_TypeOfMemberIsUnknown(member);
        }

        /// <inheritdoc />
        public NameString GetOperationName(int operation)
        {
            if (_operations.TryGetValue(operation, out SortOperation? operationConvention))
            {
                return operationConvention.Name;
            }

            throw SortConvention_OperationNameNotFound(operation);
        }

        /// <inheritdoc />
        public string? GetOperationDescription(int operationId)
        {
            if (_operations.TryGetValue(operationId, out SortOperation? operationConvention))
            {
                return operationConvention.Description;
            }

            return null;
        }

        /// <inheritdoc />
        public NameString GetArgumentName() => _argumentName;

        /// <inheritdoc cref="ISortConvention"/>
        public void ApplyConfigurations(
            ITypeReference typeReference,
            ISortInputTypeDescriptor descriptor)
        {
            if (_inputTypeConfigs.TryGetValue(
                typeReference,
                out List<ConfigureSortInputType>? configurations))
            {
                foreach (ConfigureSortInputType configure in configurations)
                {
                    configure(descriptor);
                }

                if (descriptor is SortInputTypeDescriptor inputTypeDescriptor)
                {
                    inputTypeDescriptor.CreateDefinition();
                }
            }
        }

        public void ApplyConfigurations(
            ITypeReference typeReference,
            ISortEnumTypeDescriptor descriptor)
        {
            if (_enumTypeConfigs.TryGetValue(
                typeReference,
                out List<ConfigureSortEnumType>? configurations))
            {
                foreach (ConfigureSortEnumType configure in configurations)
                {
                    configure(descriptor);
                }

                if (descriptor is SortEnumTypeDescriptor inputTypeDescriptor)
                {
                    inputTypeDescriptor.CreateDefinition();
                }
            }
        }

        public FieldMiddleware CreateExecutor<TEntityType>() =>
            _provider.CreateExecutor<TEntityType>(_argumentName);

        public bool TryGetOperationHandler(
            ITypeDiscoveryContext context,
            EnumTypeDefinition typeDefinition,
            SortEnumValueDefinition fieldDefinition,
            [NotNullWhen(true)] out ISortOperationHandler? handler)
        {
            foreach (ISortOperationHandler sortFieldHandler in _provider.OperationHandlers)
            {
                if (sortFieldHandler.CanHandle(context, typeDefinition, fieldDefinition))
                {
                    handler = sortFieldHandler;
                    return true;
                }
            }

            handler = null;
            return false;
        }

        public bool TryGetFieldHandler(
            ITypeDiscoveryContext context,
            SortInputTypeDefinition typeDefinition,
            SortFieldDefinition fieldDefinition,
            [NotNullWhen(true)] out ISortFieldHandler? handler)
        {
            foreach (ISortFieldHandler sortFieldHandler in _provider.FieldHandlers)
            {
                if (sortFieldHandler.CanHandle(context, typeDefinition, fieldDefinition))
                {
                    handler = sortFieldHandler;
                    return true;
                }
            }

            handler = null;
            return false;
        }

        private bool TryCreateSortType(
            IExtendedType runtimeType,
            [NotNullWhen(true)] out Type? type)
        {
            if (_bindings.TryGetValue(runtimeType.Source, out type))
            {
                return true;
            }

            if (runtimeType.IsArrayOrList)
            {
                return false;
            }

            if (runtimeType.Type.IsClass)
            {
                type = typeof(SortInputType<>).MakeGenericType(runtimeType.Source);
                return true;
            }

            if (_defaultBinding is {})
            {
                type = _defaultBinding;
                return true;
            }

            type = null;
            return false;
        }

        private static IReadOnlyList<ISortProviderExtension> CollectExtensions(
            IServiceProvider serviceProvider,
            SortConventionDefinition definition)
        {
            List<ISortProviderExtension> extensions = new List<ISortProviderExtension>();
            extensions.AddRange(definition.ProviderExtensions);
            foreach (var extensionType in definition.ProviderExtensionsTypes)
            {
                if (serviceProvider.GetService(extensionType) is
                    ISortProviderExtension createdExtension)
                {
                    extensions.Add(createdExtension);
                }
            }

            return extensions;
        }

        private static void MergeExtensions(
            IConventionContext context,
            ISortProviderConvention provider,
            IReadOnlyList<ISortProviderExtension> extensions)
        {
            if (provider is Convention providerConvention)
            {
                for (var m = 0; m < extensions.Count; m++)
                {
                    if (extensions[m] is ISortProviderConvention extensionConvention)
                    {
                        extensionConvention.Initialize(context);
                        extensions[m].Merge(context, providerConvention);
                        extensionConvention.OnComplete(context);
                    }
                }
            }
        }
    }
}
