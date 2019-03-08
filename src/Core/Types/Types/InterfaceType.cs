using System;
using System.Collections.Generic;
using System.Linq;
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

        public Type ClrType { get; private set; }

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
            InterfaceTypeDescriptor descriptor = InterfaceTypeDescriptor.New(
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
            context.RegisterDependencyRange(
                definition.GetDependencies(),
                TypeDependencyKind.Default);
        }

        protected override void OnCompleteType(
            ICompletionContext context,
            InterfaceTypeDefinition definition)
        {
            SyntaxNode = definition.SyntaxNode;
            ClrType = definition.ClrType;
            Fields = new FieldCollection<InterfaceField>(
                definition.Fields.Select(t => new InterfaceField(t)));

            FieldInitHelper.CompleteFields(context, definition, Fields);
            CompleteAbstractTypeResolver(
                context,
                definition.ResolveAbstractType);
        }

        private void CompleteFields(
            ICompletionContext context)
        {
            foreach (InterfaceField field in Fields)
            {
                field.CompleteField(context);
            }

            if (Fields.Count == 0)
            {
                // TODO : RESOURCES
                context.ReportError(SchemaErrorBuilder.New()
                    .SetMessage($"Interface `{Name}` has no fields declared.")
                    .SetCode(TypeErrorCodes.MissingType)
                    .SetTypeSystemObject(context.Type)
                    .AddSyntaxNode(SyntaxNode)
                    .Build());
            }
        }

        private void CompleteAbstractTypeResolver(
            ICompletionContext context,
            ResolveAbstractType resolveAbstractType)
        {
            if (resolveAbstractType == null)
            {
                // if there is no custom type resolver we will use this default
                // abstract type resolver.
                IReadOnlyCollection<ObjectType> types = null;
                _resolveAbstractType = (c, r) =>
                {
                    if (types == null)
                    {
                        types = context.GetPossibleTypes(this);
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
