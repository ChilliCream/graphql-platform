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
        private readonly IsOfType _isOfType;
        private readonly Func<IEnumerable<InterfaceType>> _resolveInterfaces;
        private readonly Dictionary<string, Field> _fields;
        private Dictionary<string, InterfaceType> _interfaces;

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

            _fields = new Dictionary<string, Field>();
            _resolveInterfaces = config.Interfaces;
            _isOfType = config.IsOfType;

            SyntaxNode = config.SyntaxNode;
            Name = config.Name;
            Description = config.Description;
            IsIntrospection = config.IsIntrospection;

            foreach (Field field in config.Fields)
            {
                _fields[field.Name] = field;
            }

        }

        public ObjectTypeDefinitionNode SyntaxNode { get; }

        public string Name { get; }

        public string Description { get; }

        public bool IsIntrospection { get; }

        public IReadOnlyDictionary<string, InterfaceType> Interfaces
        {
            get
            {
                if (_interfaces == null)
                {
                    IEnumerable<InterfaceType> interfaces = _resolveInterfaces();
                    _interfaces = (interfaces == null)
                        ? new Dictionary<string, InterfaceType>()
                        : interfaces.ToDictionary(t => t.Name);
                }
                return _interfaces;
            }
        }

        public IReadOnlyDictionary<string, Field> Fields => _fields;

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

        public bool IsIntrospection { get; set; }

        public Func<IEnumerable<InterfaceType>> Interfaces { get; set; }

        public IEnumerable<Field> Fields { get; set; }

        public IsOfType IsOfType { get; set; }
    }
}
