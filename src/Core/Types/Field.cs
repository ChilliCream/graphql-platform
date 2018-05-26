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
        private readonly Func<SchemaContext, IOutputType> _typeFactory;
        private readonly Func<SchemaContext, FieldResolverDelegate> _resolverFactory;
        private readonly Dictionary<string, InputField> _argumentMap =
            new Dictionary<string, InputField>();
        private IOutputType _type;
        private FieldResolverDelegate _resolver;

        internal Field(FieldConfig config)
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

            if (config.Type == null)
            {
                throw new ArgumentException(
                    "A field type must not be null or empty.",
                    nameof(config));
            }

            if (config.Arguments != null)
            {
                foreach (InputField argument in config.Arguments)
                {
                    if (_argumentMap.ContainsKey(argument.Name))
                    {
                        throw new ArgumentException(
                            $"The argument names are not unique -> argument: `{argument.Name}`.",
                            nameof(config));
                    }
                    else
                    {
                        _argumentMap.Add(argument.Name, argument);
                    }
                }
            }

            _typeFactory = config.Type;
            _resolverFactory = config.Resolver;

            SyntaxNode = config.SyntaxNode;
            Name = config.Name;
            Description = config.Description;
            IsIntrospection = config.IsIntrospection;
            IsDeprecated = !string.IsNullOrEmpty(config.DeprecationReason);
            DeprecationReason = config.DeprecationReason;
        }

        public FieldDefinitionNode SyntaxNode { get; }

        public string Name { get; }

        public string Description { get; }

        internal bool IsIntrospection { get; }

        public bool IsDeprecated { get; }

        public string DeprecationReason { get; }

        public IOutputType Type => _type;

        public IReadOnlyDictionary<string, InputField> Arguments => _argumentMap;

        public FieldResolverDelegate Resolver => _resolver;

        #region TypeSystemNode

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;
        IEnumerable<ITypeSystemNode> ITypeSystemNode.GetNodes()
            => _argumentMap.Values;

        #endregion

        #region Initialization

        internal void CompleteInitialization(
            SchemaContext schemaContext,
            Action<SchemaError> reportError,
            INamedType parentType)
        {
            _type = _typeFactory(schemaContext);
            if (_type == null)
            {
                reportError(new SchemaError(
                    $"The type of field `{Name}` is null.",
                    parentType));
            }

            foreach (InputField argument in _argumentMap.Values)
            {
                argument.CompleteInitialization(
                    schemaContext, reportError, parentType);
            }


            if (parentType is ObjectType)
            {
                if (_resolverFactory == null)
                {
                    reportError(new SchemaError(
                        $"The field `{Name}` of object type `{parentType.Name}` " +
                        "has no resolver factory.", parentType));
                }
                else
                {
                    _resolver = _resolverFactory(schemaContext);
                    if (_resolver == null)
                    {
                        reportError(new SchemaError(
                            $"The field `{Name}` of object type `{parentType.Name}` " +
                            "has no resolver.", parentType));
                    }
                }
            }
        }

        #endregion
    }
}
