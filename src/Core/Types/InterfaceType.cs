using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public class InterfaceType
        : IComplexOutputType
        , INeedsInitialization

    {
        private ResolveAbstractType _resolveAbstractType;

        public InterfaceType(Action<IInterfaceTypeDescriptor> configure)
            : this(ExecuteConfigure(configure))
        {
        }

        internal InterfaceType(Func<InterfaceTypeDescription> descriptionFactory)
            : this(DescriptorHelpers.ExecuteFactory(descriptionFactory))
        {
        }

        internal InterfaceType(InterfaceTypeDescription interfaceTypeDescription)
        {
            if (string.IsNullOrEmpty(interfaceTypeDescription.Name))
            {
                throw new ArgumentException(
                    "The type name must not be null or empty.");
            }

            _resolveAbstractType = interfaceTypeDescription.ResolveAbstractType;

            SyntaxNode = interfaceTypeDescription.SyntaxNode;
            Name = interfaceTypeDescription.Name;
            Description = interfaceTypeDescription.Description;
            Fields = new FieldCollection<InterfaceField>(
                interfaceTypeDescription.Fields.Select(t => new InterfaceField(t)));
        }

        private static InterfaceTypeDescription ExecuteConfigure(
            Action<IInterfaceTypeDescriptor> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            InterfaceTypeDescriptor descriptor = new InterfaceTypeDescriptor();
            configure(descriptor);
            return descriptor.CreateDescription();
        }

        public TypeKind Kind { get; } = TypeKind.Interface;

        public InterfaceTypeDefinitionNode SyntaxNode { get; }

        public string Name { get; }

        public string Description { get; }

        public FieldCollection<InterfaceField> Fields { get; }

        IFieldCollection<IOutputField> IComplexOutputType.Fields => Fields;

        public ObjectType ResolveType(IResolverContext context, object resolverResult)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return _resolveAbstractType.Invoke(context, resolverResult);
        }

        #region Configuration

        protected virtual void Configure(IInterfaceTypeDescriptor descriptor) { }

        #endregion

        #region Initialization

        void INeedsInitialization.RegisterDependencies(
            ISchemaContext schemaContext,
            Action<SchemaError> reportError)
        {
            foreach (InterfaceField field in Fields)
            {
                field.RegisterDependencies(schemaContext, reportError, this);
            }
        }

        void INeedsInitialization.CompleteType(
            ISchemaContext schemaContext,
            Action<SchemaError> reportError)
        {
            CompleteAbstractTypeResolver(schemaContext.Types);

            foreach (InterfaceField field in Fields)
            {
                field.CompleteField(schemaContext, reportError, this);
            }

            // TODO : report error that fields are empty
        }

        private void CompleteAbstractTypeResolver(
            ITypeRegistry typeRegistry)
        {
            if (_resolveAbstractType == null)
            {
                // if there is now custom type resolver we will use this default
                // abstract type resolver.
                List<ObjectType> types = null;
                _resolveAbstractType = (c, r) =>
                {
                    if (types == null)
                    {
                        types = typeRegistry.GetTypes()
                            .OfType<ObjectType>()
                            .Where(t => t.Interfaces.ContainsKey(Name))
                            .ToList();
                    }

                    foreach (ObjectType type in types)
                    {
                        if (type.IsOfType(c, r))
                        {
                            return type;
                        }
                    }

                    return null; // todo: should we throw instead?
                };
            }
        }

        #endregion
    }
}
