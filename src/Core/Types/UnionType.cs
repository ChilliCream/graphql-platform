using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly ResolveAbstractType _typeResolver;
        private readonly Func<ITypeRegistry, IEnumerable<ObjectType>> _typesFactory;
        private readonly Dictionary<string, ObjectType> _typeMap =
            new Dictionary<string, ObjectType>();
        private readonly IReadOnlyCollection<TypeInfo> _typeInfos;

        public UnionType()
        {
            UnionTypeDescriptor descriptor = new UnionTypeDescriptor();
            Configure(descriptor);

            if (string.IsNullOrEmpty(descriptor.Name))
            {
                throw new ArgumentException(
                    "A union type name must not be null or empty.");
            }

            if (descriptor.Types == null)
            {
                throw new ArgumentException(
                    "A union type must have a set of types.");
            }

            if (descriptor.ResolveAbstractType == null)
            {
                throw new ArgumentException(
                    "A Union type must define one or more unique member types.");
            }

            _typesFactory = r => descriptor.Types
                .Select(t => t.TypeFactory(r))
                .Cast<ObjectType>();
            _typeInfos = descriptor.Types;
            _typeResolver = descriptor.ResolveAbstractType;

            Name = descriptor.Name;
            Description = descriptor.Description;
        }

        internal UnionType(UnionTypeConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(config.Name))
            {
                throw new ArgumentException(
                    "A union type name must not be null or empty.",
                    nameof(config));
            }

            if (config.Types == null)
            {
                throw new ArgumentException(
                    "A union type must have a set of types.",
                    nameof(config));
            }

            if (config.ResolveAbstractType == null)
            {
                throw new ArgumentException(
                    "A Union type must define one or more unique member types.",
                    nameof(config));
            }

            _typesFactory = config.Types;
            _typeResolver = config.ResolveAbstractType;

            SyntaxNode = config.SyntaxNode;
            Name = config.Name;
            Description = config.Description;
        }

        public UnionTypeDefinitionNode SyntaxNode { get; }

        public string Name { get; }

        public string Description { get; }

        public IReadOnlyDictionary<string, ObjectType> Types => _typeMap;

        public ObjectType ResolveType(IResolverContext context, object resolverResult)
            => _typeResolver(context, resolverResult);

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

        void INeedsInitialization.RegisterDependencies(
            ISchemaContextR schemaContext, Action<SchemaError> reportError)
        {
            foreach (TypeInfo typeInfo in _typeInfos)
            {
                schemaContext.Types.RegisterType(typeInfo.NativeNamedType);
            }
        }

        void INeedsInitialization.CompleteType(ISchemaContextR schemaContext, Action<SchemaError> reportError)
        {
            if (_typesFactory == null)
            {
                reportError(new SchemaError(
                    "A Union type must define one or more unique member types.",
                    this));
            }
            else
            {
                foreach (ObjectType memberType in _typesFactory(schemaContext.Types))
                {
                    _typeMap[memberType.Name] = memberType;
                }
            }
        }

        #endregion
    }
}
