using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public class UnionType
        : INamedType
        , IOutputType
        , INullableType
        , ITypeSystemNode
        , INeedsInitialization
    {
        private readonly Dictionary<string, ObjectType> _typeMap =
            new Dictionary<string, ObjectType>();
        private ImmutableList<TypeReference> _types;
        private ResolveAbstractType _resolveAbstractType;

        protected UnionType()
        {
            Initialize(Configure);
        }

        public UnionType(Action<IUnionTypeDescriptor> configure)
        {
            Initialize(configure);
        }

        public TypeKind Kind { get; } = TypeKind.Union;

        public UnionTypeDefinitionNode SyntaxNode { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public IReadOnlyDictionary<string, ObjectType> Types => _typeMap;

        public ObjectType ResolveType(IResolverContext context, object resolverResult)
            => _resolveAbstractType(context, resolverResult);

        #region Configuration

        protected virtual void Configure(IUnionTypeDescriptor descriptor) { }

        #endregion

        #region ITypeSystemNode

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;

        IEnumerable<ITypeSystemNode> ITypeSystemNode.GetNodes()
        {
            return Types.Values;
        }

        #endregion

        #region Initialization

        private void Initialize(Action<IUnionTypeDescriptor> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            UnionTypeDescriptor descriptor = new UnionTypeDescriptor(GetType());
            configure(descriptor);
            Initialize(descriptor);
        }

        private void Initialize(UnionTypeDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (string.IsNullOrEmpty(descriptor.Name))
            {
                throw new ArgumentException(
                    "A union type name must not be null or empty.");
            }

            _types = descriptor.Types;
            _resolveAbstractType = descriptor.ResolveAbstractType;

            Name = descriptor.Name;
            Description = descriptor.Description;
        }

        void INeedsInitialization.RegisterDependencies(
            ISchemaContext schemaContext, Action<SchemaError> reportError)
        {
            if (_types != null)
            {
                foreach (TypeReference typeReference in _types)
                {
                    schemaContext.Types.RegisterType(typeReference);
                }
            }
        }

        void INeedsInitialization.CompleteType(
            ISchemaContext schemaContext,
            Action<SchemaError> reportError)
        {
            CompleteTypes(schemaContext.Types, reportError);
            CompleteResolveAbstractType();
        }

        private void CompleteTypes(
            ITypeRegistry typeRegistry,
            Action<SchemaError> reportError)
        {
            if (_types != null)
            {
                foreach (ObjectType memberType in _types
                    .Select(t => typeRegistry.GetType<ObjectType>(t))
                    .Where(t => t != null))
                {
                    _typeMap[memberType.Name] = memberType;
                }
            }

            if (_typeMap.Count == 0)
            {
                reportError(new SchemaError(
                    "A Union type must define one or more unique member types.",
                    this));
            }
        }

        private void CompleteResolveAbstractType()
        {
            if (_resolveAbstractType == null)
            {
                // if there is now custom type resolver we will use this default
                // abstract type resolver.
                _resolveAbstractType = (c, r) =>
                {
                    foreach (ObjectType type in _typeMap.Values)
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
