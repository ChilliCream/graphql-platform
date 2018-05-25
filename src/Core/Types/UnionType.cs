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
        , ITypeInitializer
    {
        private readonly ResolveType _typeResolver;
        private readonly Func<IEnumerable<ObjectType>> _typesFactory;
        private readonly Dictionary<string, ObjectType> _typeMap =
            new Dictionary<string, ObjectType>();

        public UnionType(UnionTypeConfig config)
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

            if (config.TypeResolver == null)
            {
                throw new ArgumentException(
                    "A Union type must define one or more unique member types.",
                    nameof(config));
            }

            _typesFactory = config.Types;
            _typeResolver = config.TypeResolver;

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

        #region ITypeSystemNode

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;


        IEnumerable<ITypeSystemNode> ITypeSystemNode.GetNodes()
        {
            return Types.Values;
        }

        #endregion

        #region Initialization

        void ITypeInitializer.CompleteInitialization(
            ISchemaContext schemaContext,
            Action<SchemaError> reportError)
        {
            ObjectType[] memberTypes = _typesFactory()?.ToArray()
                ?? Array.Empty<ObjectType>();

            if (memberTypes.Length == 0)
            {
                reportError(new SchemaError(
                    "A Union type must define one or more unique member types.",
                    this));
            }

            foreach (ObjectType memberType in memberTypes)
            {
                if (_typeMap.ContainsKey(memberType.Name))
                {
                    reportError(new SchemaError(
                        "The set of member types of the union type {Name} is not unique.",
                        this));
                }
                else
                {
                    _typeMap[memberType.Name] = memberType;
                }
            }
        }

        #endregion
    }

    public class UnionTypeConfig
    {
        public UnionTypeDefinitionNode SyntaxNode { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public Func<IEnumerable<ObjectType>> Types { get; set; }

        public ResolveType TypeResolver { get; set; }
    }
}
