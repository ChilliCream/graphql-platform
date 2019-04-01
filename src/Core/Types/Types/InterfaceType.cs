using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class InterfaceType
        : NamedTypeBase<InterfaceTypeDefinition>
        , IComplexOutputType
        , IHasClrType
        , INamedType
    {
        private readonly Action<IInterfaceTypeDescriptor> _configure;
        private ResolveAbstractType _resolveAbstractType;

        protected InterfaceType()
        {
            _configure = Configure;
        }

        public InterfaceType(Action<IInterfaceTypeDescriptor> configure)
        {
            _configure = configure
                ?? throw new ArgumentNullException(nameof(configure));
        }

        public override TypeKind Kind => TypeKind.Interface;

        public InterfaceTypeDefinitionNode SyntaxNode { get; private set; }

        public FieldCollection<InterfaceField> Fields { get; private set; }

        IFieldCollection<IOutputField> IComplexOutputType.Fields => Fields;

        public ObjectType ResolveType(
            IResolverContext context,
            object resolverResult)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return _resolveAbstractType.Invoke(context, resolverResult);
        }

        #region Initialization

        protected override InterfaceTypeDefinition CreateDefinition(
            IInitializationContext context)
        {
            var descriptor = InterfaceTypeDescriptor.New(
                DescriptorContext.Create(context.Services),
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

            RegisterDependencies(context, definition);
        }

        private void RegisterDependencies(
           IInitializationContext context,
           InterfaceTypeDefinition definition)
        {
            var dependencies = new List<ITypeReference>();

            context.RegisterDependencyRange(
                definition.Fields.Select(t => t.Type),
                TypeDependencyKind.Default);

            context.RegisterDependencyRange(
                definition.Fields.SelectMany(t => t.Arguments)
                    .Select(t => t.Type),
                TypeDependencyKind.Completed);

            context.RegisterDependencyRange(
                definition.Fields.SelectMany(t => t.Directives)
                    .Select(t => t.TypeReference),
                TypeDependencyKind.Completed);
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
            FieldInitHelper.CompleteFields(context, definition, Fields);
        }       

        private void CompleteAbstractTypeResolver(
            ICompletionContext context,
            ResolveAbstractType resolveAbstractType)
        {
            if (resolveAbstractType == null)
            {
                Func<ISchema> schemaResolver = context.GetSchemaResolver();

                // if there is no custom type resolver we will use this default
                // abstract type resolver.
                IReadOnlyCollection<ObjectType> types = null;
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

        #endregion
    }
}
