using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public class InterfaceType
        : INamedType
        , IOutputType
        , INullableType
        , ITypeSystemNode
        , ITypeInitializer
        , IHasFields
    {
        private readonly ResolveType _typeResolver;
        private readonly IEnumerable<Field> _fields;
        private readonly Dictionary<string, Field> _fieldMap =
            new Dictionary<string, Field>();

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

            if (config.Fields == null)
            {
                throw new ArgumentException(
                    "An interface type must provide fields.",
                    nameof(config));
            }

            _typeResolver = config.TypeResolver;
            _fields = config.Fields;

            SyntaxNode = config.SyntaxNode;
            Name = config.Name;
            Description = config.Description;
        }

        public InterfaceTypeDefinitionNode SyntaxNode { get; }

        public string Name { get; }

        public string Description { get; }

        public IReadOnlyDictionary<string, Field> Fields { get; }

        public ObjectType ResolveType(IResolverContext context, object resolverResult)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return _typeResolver.Invoke(context, resolverResult);
        }

        #region TypeSystemNode

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;

        IEnumerable<ITypeSystemNode> ITypeSystemNode.GetNodes() => Fields.Values;

        #endregion

        #region Initialization

        void ITypeInitializer.CompleteInitialization(Action<SchemaError> reportError)
        {
            Field[] fields = _fields.ToArray();
            if (fields.Length == 0)
            {
                reportError(new SchemaError(
                    $"The interface type {Name} has no fields.",
                    this));
            }

            foreach (Field field in fields)
            {
                field.CompleteInitialization(reportError, this);
                if (_fieldMap.ContainsKey(field.Name))
                {
                    reportError(new SchemaError(
                        $"The field name of field {field.Name} " +
                        $"is not unique within {Name}.",
                        this));
                }
                else
                {
                    _fieldMap.Add(field.Name, field);
                }
            }
        }

        #endregion
    }

    public class InterfaceTypeConfig
        : INamedTypeConfig
    {
        public InterfaceTypeDefinitionNode SyntaxNode { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public IEnumerable<Field> Fields { get; set; }

        public ResolveType TypeResolver { get; set; }
    }
}
