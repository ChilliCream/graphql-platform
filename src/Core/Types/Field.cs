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
        private readonly FieldConfig _config;
        private IOutputType _type;
        private IReadOnlyDictionary<string, InputField> _arguments;
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

            _config = config;
            SyntaxNode = config.SyntaxNode;
            Name = config.Name;
            Description = config.Description;
        }

        public FieldDefinitionNode SyntaxNode { get; }

        public string Name { get; }

        public string Description { get; }

        public IOutputType Type
        {
            get
            {
                if (_type == null)
                {
                    _type = _config.Type();
                    if (_type == null)
                    {
                        throw new InvalidOperationException(
                            "The field return type mustn't be null.");
                    }
                }
                return _type;
            }
        }

        public IReadOnlyDictionary<string, InputField> Arguments
        {
            get
            {
                if (_arguments == null)
                {
                    var arguments = _config.Arguments();
                    _arguments = (arguments == null)
                        ? new Dictionary<string, InputField>()
                        : arguments;
                }
                return _arguments;
            }
        }

        public FieldResolverDelegate Resolver
        {
            get
            {
                if (_resolver == null)
                {
                    _resolver = _config.Resolver();
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

        public Func<IOutputType> Type { get; set; }

        public Func<IReadOnlyDictionary<string, InputField>> Arguments { get; set; }

        public Func<FieldResolverDelegate> Resolver { get; set; }
    }
}