using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public class Field
        : ITypeSystemNode
    {
        private readonly Dictionary<string, InputField> _arguments;
        private readonly Func<IOutputType> _resolveType;
        private readonly Func<FieldResolverDelegate> _resolveResolver;
        private IOutputType _type;
        private FieldResolverDelegate _resolver;

        public Field(FieldConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(config.Name))
            {
                throw new ArgumentException(
                    "A field name must not be null or empty.",
                    nameof(config));
            }

            _arguments = config.Arguments.ToDictionary(t => t.Name);
            _resolveType = config.Type;
            _resolveResolver = config.Resolver;

            SyntaxNode = config.SyntaxNode;
            Name = config.Name;
            Description = config.Description;
            IsIntrospection = config.IsIntrospection;
        }

        public FieldDefinitionNode SyntaxNode { get; }

        public string Name { get; }

        public string Description { get; }

        internal bool IsIntrospection { get; }

        public IOutputType Type
        {
            get
            {
                if (_type == null)
                {
                    _type = _resolveType();
                    if (_type == null)
                    {
                        throw new InvalidOperationException(
                            "The field return type mustn't be null.");
                    }
                }
                return _type;
            }
        }

        public IReadOnlyDictionary<string, InputField> Arguments => _arguments;

        public FieldResolverDelegate Resolver
        {
            get
            {
                if (_resolver == null)
                {
                    _resolver = _resolveResolver();
                }
                return _resolver;
            }
        }

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;
        IEnumerable<ITypeSystemNode> ITypeSystemNode.GetNodes()
            => Enumerable.Empty<ITypeSystemNode>();
    }

    public class FieldConfig
    {
        public FieldDefinitionNode SyntaxNode { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        internal bool IsIntrospection { get; set; }

        public Func<IOutputType> Type { get; set; }

        public IEnumerable<InputField> Arguments { get; set; }

        public Func<FieldResolverDelegate> Resolver { get; set; }
    }
}
