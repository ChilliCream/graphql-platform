using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public class ObjectType
        : IOutputType
        , INamedType
        , INullableType
        , ITypeSystemNode
    {
        private readonly ObjectTypeConfig _config;
        private readonly IsOfType _isOfType;
        private IReadOnlyDictionary<string, InterfaceType> _interfaces;
        private IReadOnlyDictionary<string, Field> _fields;

        public ObjectType(ObjectTypeConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(config.Name))
            {
                throw new ArgumentException(
                    "A type name must not be null or empty.",
                    nameof(config));
            }

            _config = config;
            _isOfType = config.IsOfType;
            Name = config.Name;
            Description = config.Description;
            SyntaxNode = config.SyntaxNode;
        }

        public ObjectTypeDefinitionNode SyntaxNode { get; }

        public string Name { get; }

        public string Description { get; }

        public IReadOnlyDictionary<string, InterfaceType> Interfaces
        {
            get
            {
                if (_interfaces == null)
                {
                    var interfaces = _config.Interfaces();
                    _interfaces = (interfaces == null)
                        ? new Dictionary<string, InterfaceType>()
                        : interfaces;
                }
                return _interfaces;
            }
        }

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

        public bool IsOfType(IResolverContext context, object resolverResult)
        {
            if (_isOfType == null)
            {
                throw new NotImplementedException(
                    "The fallback resolver logic is not yet implemented.");
            }
            return _isOfType(context, resolverResult);
        }

        #region ITypeSystemNode

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;

        IEnumerable<ITypeSystemNode> ITypeSystemNode.GetNodes()
        {
            foreach (InterfaceType node in Interfaces.Values)
            {
                yield return node;
            }

            foreach (Field node in Fields.Values)
            {
                yield return node;
            }
        }

        #endregion
    }

    public class ObjectTypeConfig
    {
        public ObjectTypeDefinitionNode SyntaxNode { get; set; }
        public string Name { get; set; }

        public string Description { get; set; }

        public Func<IReadOnlyDictionary<string, InterfaceType>> Interfaces { get; set; }

        public Func<IReadOnlyDictionary<string, Field>> Fields { get; set; }

        public IsOfType IsOfType { get; set; }
    }
}