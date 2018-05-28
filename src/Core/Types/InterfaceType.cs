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
        , INeedsInitialization
        , IHasFields
    {
        private readonly ResolveAbstractType _resolveAbstractType;
        private readonly Dictionary<string, Field> _fieldMap =
            new Dictionary<string, Field>();

        public InterfaceType()
        {
            InterfaceTypeDescriptor descriptor = new InterfaceTypeDescriptor();
            Configure(descriptor);

            if (string.IsNullOrEmpty(descriptor.Name))
            {
                throw new ArgumentException(
                    "The type name must not be null or empty.");
            }

            if (descriptor.Fields.Count == 0)
            {
                throw new ArgumentException(
                    $"The interface type `{Name}` has no fields.");
            }

            foreach (Field field in descriptor.Fields.Select(t => t.CreateField()))
            {
                _fieldMap[field.Name] = field;
            }

            _resolveAbstractType = descriptor.ResolveAbstractType;

            Name = descriptor.Name;
            Description = descriptor.Description;
        }

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

            _resolveAbstractType = config.ResolveAbstractType;

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

            return _resolveAbstractType.Invoke(context, resolverResult);
        }

        #region Configuration

        protected virtual void Configure(IInterfaceTypeDescriptor descriptor) { }

        #endregion

        #region TypeSystemNode

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;

        IEnumerable<ITypeSystemNode> ITypeSystemNode.GetNodes() => Fields.Values;

        #endregion

        #region Initialization

        void INeedsInitialization.RegisterDependencies(
            ISchemaContextR schemaContext, Action<SchemaError> reportError)
        {
            foreach (Field field in _fieldMap.Values)
            {
                field.RegisterDependencies(schemaContext, reportError, this);
            }
        }

        void INeedsInitialization.CompleteType(
            ISchemaContextR schemaContext, Action<SchemaError> reportError)
        {
            foreach (Field field in _fieldMap.Values)
            {
                field.CompleteField(schemaContext, reportError, this);
            }
        }

        #endregion
    }
}
