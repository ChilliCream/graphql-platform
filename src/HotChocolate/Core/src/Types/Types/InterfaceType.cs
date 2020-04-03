using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    public class InterfaceType
        : NamedTypeBase<InterfaceTypeDefinition>
        , IComplexOutputType
        , IHasClrType
        , INamedType
    {
        private readonly List<InterfaceType> _interfaces = new List<InterfaceType>();
        private readonly Action<IInterfaceTypeDescriptor> _configure;
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

        public IReadOnlyList<InterfaceType> Interfaces => _interfaces;

        public InterfaceTypeDefinitionNode? SyntaxNode { get; private set; }

        public FieldCollection<InterfaceField> Fields { get; private set; }

        IFieldCollection<IOutputField> IComplexOutputType.Fields => Fields;

        public bool IsImplementing(NameString interfaceTypeName) =>
            _interfaces.Any(t => t.Name.Equals(interfaceTypeName));

        public bool IsImplementing(InterfaceType interfaceType) =>
            _interfaces.Contains(interfaceType);

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

        public ObjectType ResolveType(IResolverContext context, object resolverResult)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return _resolveAbstractType!.Invoke(context, resolverResult);
        }

        protected override InterfaceTypeDefinition CreateDefinition(
            IInitializationContext context)
        {
            var descriptor = InterfaceTypeDescriptor.FromSchemaType(
                context.DescriptorContext,
                GetType());
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IInterfaceTypeDescriptor descriptor)
        {
        }

        protected override void OnRegisterDependencies(
            IInitializationContext context,
            InterfaceTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);
            context.RegisterDependencies(definition);
            SetTypeIdentity(typeof(InterfaceType<>));
        }

        protected override void OnCompleteType(
            ICompletionContext context,
            InterfaceTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            SyntaxNode = definition.SyntaxNode;
            Fields = new FieldCollection<InterfaceField>(
                definition.Fields.Select(t => new InterfaceField(t)));

            CompleteAbstractTypeResolver(
                context,
                definition.ResolveAbstractType);

            CompleteInterfacesHelper.Complete(
                context, definition, ClrType, _interfaces, this, SyntaxNode);

            FieldInitHelper.CompleteFields(context, definition, Fields);
        }

        private void CompleteAbstractTypeResolver(
            ICompletionContext context,
            ResolveAbstractType? resolveAbstractType)
        {
            if (resolveAbstractType == null)
            {
                Func<ISchema> schemaResolver = context.GetSchemaResolver();

                // if there is no custom type resolver we will use this default
                // abstract type resolver.
                IReadOnlyCollection<ObjectType>? types = null;
                _resolveAbstractType = (c, r) =>
                {
                    if (types == null)
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
