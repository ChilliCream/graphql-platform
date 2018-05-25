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
        private readonly Dictionary<string, Field> _fieldMap =
            new Dictionary<string, Field>();

        internal InterfaceType(InterfaceTypeConfig config)
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

            Field[] fields = config.Fields?.ToArray()
                ?? Array.Empty<Field>();
            if (fields.Length == 0)
            {
                throw new ArgumentException(
                    $"The interface type `{Name}` has no fields.",
                    nameof(config));
            }

            foreach (Field field in fields)
            {
                if (_fieldMap.ContainsKey(field.Name))
                {
                    throw new ArgumentException(
                        $"The field name `{field.Name}` " +
                        $"is not unique within `{Name}`.",
                        nameof(config));
                }
                else
                {
                    _fieldMap.Add(field.Name, field);
                }
            }

            _typeResolver = config.TypeResolver;

            SyntaxNode = config.SyntaxNode;
            Name = config.Name;
            Description = config.Description;
        }

        public InterfaceTypeDefinitionNode SyntaxNode { get; }

        public string Name { get; }

        public string Description { get; }

        public IReadOnlyDictionary<string, Field> Fields => _fieldMap;

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

        void ITypeInitializer.CompleteInitialization(
            SchemaContext schemaContext,
            Action<SchemaError> reportError)
        {
            foreach (Field field in _fieldMap.Values)
            {
                field.CompleteInitialization(schemaContext, reportError, this);
            }
        }

        #endregion
    }
}
