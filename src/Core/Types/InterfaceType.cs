using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public class InterfaceType
        : IOutputType
        , INamedType
        , INullableType
        , ITypeSystemNode
        , IHasFields
    {
        private readonly InterfaceTypeConfig _config;
        private readonly ResolveType _typeResolver;
        private IReadOnlyDictionary<string, Field> _fields;

        public InterfaceType(InterfaceTypeConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(config.Name))
            {
                throw new ArgumentException(
                    "An interface type name must not be null or empty.",
                    nameof(config));
            }

            _config = config;
            _typeResolver = config.TypeResolver;
            SyntaxNode = config.SyntaxNode;
            Name = config.Name;
            Description = config.Description;
        }

        public InterfaceTypeDefinitionNode SyntaxNode { get; }

        public string Name { get; }

        public string Description { get; }

        public IReadOnlyDictionary<string, Field> Fields
        {
            get
            {
                if (_fields == null)
                {
                    var fields = _config.Fields();
                    if (fields == null)
                    {
                        throw new InvalidOperationException(
                            "The fields collection mustn't be null.");
                    }
                    _fields = fields;
                }
                return _fields;
            }
        }

        public ObjectType ResolveType(IResolverContext context, object resolverResult)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return _typeResolver.Invoke(context, resolverResult);
        }

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;

        IEnumerable<ITypeSystemNode> ITypeSystemNode.GetNodes() => Fields.Values;
    }

    public class InterfaceTypeConfig
    {
        public InterfaceTypeDefinitionNode SyntaxNode { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public Func<IReadOnlyDictionary<string, Field>> Fields { get; set; }

        public ResolveType TypeResolver { get; set; }
    }
}
