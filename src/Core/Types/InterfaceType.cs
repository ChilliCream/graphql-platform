using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public class InterfaceType
        : INamedType
        , IOutputType
        , INullableType
        , ITypeSystemNode
        , INeedsInitialization
        , IHasFields
    {
        private readonly Dictionary<string, Field> _fieldMap =
            new Dictionary<string, Field>();
        private ResolveAbstractType _resolveAbstractType;

        protected InterfaceType()
        {
            Initialize(Configure);
        }

        public InterfaceType(Action<IInterfaceTypeDescriptor> configure)
        {
            Initialize(configure);
        }

        public TypeKind Kind { get; } = TypeKind.Interface;

        public InterfaceTypeDefinitionNode SyntaxNode { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public IReadOnlyDictionary<string, Field> Fields => _fieldMap;

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

        #region TypeSystemNode

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;

        IEnumerable<ITypeSystemNode> ITypeSystemNode.GetNodes() => Fields.Values;

        #endregion

        #region Initialization

        private void Initialize(Action<IInterfaceTypeDescriptor> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            InterfaceTypeDescriptor descriptor =
                new InterfaceTypeDescriptor(GetType());
            configure(descriptor);
            Initialize(descriptor);
        }

        private void Initialize(InterfaceTypeDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (string.IsNullOrEmpty(descriptor.Name))
            {
                throw new ArgumentException(
                    "The type name must not be null or empty.");
            }

            foreach (Field field in descriptor.Fields
                .Select(t => new Field(t)))
            {
                _fieldMap[field.Name] = field;
            }

            _resolveAbstractType = descriptor.ResolveAbstractType;

            SyntaxNode = descriptor.SyntaxNode;
            Name = descriptor.Name;
            Description = descriptor.Description;
        }

        void INeedsInitialization.RegisterDependencies(
            ISchemaContext schemaContext,
            Action<SchemaError> reportError)
        {
            foreach (Field field in _fieldMap.Values)
            {
                field.RegisterDependencies(schemaContext, reportError, this);
            }
        }

        void INeedsInitialization.CompleteType(
            ISchemaContext schemaContext,
            Action<SchemaError> reportError)
        {
            CompleteAbstractTypeResolver(schemaContext.Types);

            foreach (Field field in _fieldMap.Values)
            {
                field.CompleteField(schemaContext, reportError, this);
            }
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
