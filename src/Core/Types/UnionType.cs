using System;
using System.Collections.Generic;
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
        private Func<ITypeRegistry, IEnumerable<ObjectType>> _typesFactory;
        private IReadOnlyCollection<TypeInfo> _typeInfos;
        private ResolveAbstractType _typeResolver;

        protected UnionType()
        {
            Initialize(Configure);
        }

        public UnionType(Action<IUnionTypeDescriptor> configure)
        {
            Initialize(configure);
        }

        internal UnionType(UnionTypeConfig config)
        {
            Initialize(config);
        }

        public TypeKind Kind { get; } = TypeKind.Union;

        public UnionTypeDefinitionNode SyntaxNode { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

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

        private void Initialize(Action<IUnionTypeDescriptor> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            UnionTypeDescriptor descriptor = new UnionTypeDescriptor(GetType());
            configure(descriptor);

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

            _typesFactory = r => descriptor.Types
                .Select(t => t.TypeFactory(r))
                .Cast<ObjectType>();
            _typeInfos = descriptor.Types;
            _typeResolver = descriptor.ResolveAbstractType;

            Name = descriptor.Name;
            Description = descriptor.Description;
        }

        private void Initialize(UnionTypeConfig config)
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

            _typesFactory = config.Types;
            _typeResolver = config.ResolveAbstractType;

            SyntaxNode = config.SyntaxNode;
            Name = config.Name;
            Description = config.Description;
        }

        void INeedsInitialization.RegisterDependencies(
            ISchemaContext schemaContext, Action<SchemaError> reportError)
        {
            if (_typeInfos != null)
            {
                foreach (TypeInfo typeInfo in _typeInfos)
                {
                    schemaContext.Types.RegisterType(typeInfo.NativeNamedType);
                }
            }
        }

        void INeedsInitialization.CompleteType(ISchemaContext schemaContext, Action<SchemaError> reportError)
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

            if (_typeResolver == null)
            {
                // if there is now custom type resolver we will use this default
                // abstract type resolver.
                _typeResolver = (c, r) =>
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
