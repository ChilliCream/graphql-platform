using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Types.CompleteInterfacesHelper;

#nullable enable

namespace HotChocolate.Types
{
    public class InterfaceType
        : NamedTypeBase<InterfaceTypeDefinition>
        , IInterfaceType
    {
        private InterfaceType[] _implements = Array.Empty<InterfaceType>();
        private Action<IInterfaceTypeDescriptor>? _configure;
        private ResolveAbstractType? _resolveAbstractType;

        protected InterfaceType()
        {
            _configure = Configure;
            Fields = FieldCollection<InterfaceField>.Empty;
        }

        public InterfaceType(Action<IInterfaceTypeDescriptor> configure)
        {
            _configure = configure ?? throw new ArgumentNullException(nameof(configure));
            Fields = FieldCollection<InterfaceField>.Empty;
        }

        public override TypeKind Kind => TypeKind.Interface;

        ISyntaxNode? IHasSyntaxNode.SyntaxNode => SyntaxNode;

        public IReadOnlyList<InterfaceType> Implements => _implements;

        IReadOnlyList<IInterfaceType> IComplexOutputType.Implements => _implements;

        public InterfaceTypeDefinitionNode? SyntaxNode { get; private set; }

        public FieldCollection<InterfaceField> Fields { get; private set; }

        IFieldCollection<IOutputField> IComplexOutputType.Fields => Fields;

        public bool IsImplementing(NameString interfaceTypeName) =>
            _implements.Any(t => t.Name.Equals(interfaceTypeName));

        public bool IsImplementing(InterfaceType interfaceType) =>
            Array.IndexOf(_implements, interfaceType) != -1;

        public bool IsImplementing(IInterfaceType interfaceType) =>
            interfaceType is InterfaceType i &&
            Array.IndexOf(_implements, i) != -1;

        public override bool IsAssignableFrom(INamedType namedType)
        {
            switch (namedType.Kind)
            {
                case TypeKind.Interface:
                    return ReferenceEquals(namedType, this) ||
                        ((InterfaceType)namedType).IsImplementing(this);

                case TypeKind.Object:
                    return ((ObjectType)namedType).IsImplementing(this);

                default:
                    return false;
            }
        }

        public ObjectType? ResolveConcreteType(
            IResolverContext context,
            object resolverResult)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return _resolveAbstractType!.Invoke(context, resolverResult);
        }

        IObjectType? IInterfaceType.ResolveConcreteType(
            IResolverContext context,
            object resolverResult) =>
            ResolveConcreteType(context, resolverResult);

        protected override InterfaceTypeDefinition CreateDefinition(
            ITypeDiscoveryContext context)
        {
            var descriptor =
                InterfaceTypeDescriptor.FromSchemaType(context.DescriptorContext, GetType());

            _configure!(descriptor);
            _configure = null;

            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IInterfaceTypeDescriptor descriptor)
        {
        }

        protected override void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            InterfaceTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);
            context.RegisterDependencies(definition);
            SetTypeIdentity(typeof(InterfaceType<>));
        }

        protected override void OnCompleteType(
            ITypeCompletionContext context,
            InterfaceTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            SyntaxNode = definition.SyntaxNode;
            var sortFieldsByName = context.DescriptorContext.Options.SortFieldsByName;

            Fields = FieldCollection<InterfaceField>.From(
                definition.Fields.Where(t => !t.Ignore).Select(
                    t => new InterfaceField(
                        t,
                        new FieldCoordinate(Name, t.Name),
                        sortFieldsByName)),
                sortFieldsByName);

            CompleteAbstractTypeResolver(context, definition.ResolveAbstractType);

            IReadOnlyList<ITypeReference> interfaces = definition.GetInterfaces();

            if (interfaces.Count > 0)
            {
                var implements = new List<InterfaceType>();

                CompleteInterfaces(
                    context,
                    interfaces,
                    RuntimeType,
                    implements,
                    this,
                    SyntaxNode);

                _implements = implements.ToArray();
            }

            FieldInitHelper.CompleteFields(context, definition, Fields);
        }

        private void CompleteAbstractTypeResolver(
            ITypeCompletionContext context,
            ResolveAbstractType? resolveAbstractType)
        {
            if (resolveAbstractType is null)
            {
                Func<ISchema> schemaResolver = context.GetSchemaResolver();

                // if there is no custom type resolver we will use this default
                // abstract type resolver.
                IReadOnlyCollection<ObjectType>? types = null;
                _resolveAbstractType = (c, r) =>
                {
                    if (types is null)
                    {
                        ISchema schema = schemaResolver.Invoke();
                        types = schema.GetPossibleTypes(this);
                    }

                    foreach (ObjectType type in types)
                    {
                        if (type.IsOfType(c, r))
                        {
                            return type;
                        }
                    }

                    return null;
                };
            }
            else
            {
                _resolveAbstractType = resolveAbstractType;
            }
        }
    }
}
