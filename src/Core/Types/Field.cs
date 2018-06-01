using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public class Field
        : ITypeSystemNode
    {
        private readonly Func<ITypeRegistry, IOutputType> _typeFactory;
        private readonly Func<IResolverRegistry, FieldResolverDelegate> _resolverFactory;
        private readonly Dictionary<string, InputField> _argumentMap =
            new Dictionary<string, InputField>();
        private MemberInfo _member;
        private IOutputType _type;
        private Type _nativeNamedType;
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

            _member = config.Member;
            _typeFactory = config.Type;
            _nativeNamedType = config.NativeNamedType;
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

        internal void RegisterDependencies(
            ISchemaContext schemaContext,
            Action<SchemaError> reportError,
            INamedType parentType)
        {
            if (_member != null)
            {
                schemaContext.Resolvers.RegisterResolver(
                    new MemberResolverBinding(parentType.Name, Name, _member));
            }

            if (_nativeNamedType != null)
            {
                schemaContext.Types.RegisterType(_nativeNamedType);
            }

            foreach (InputField argument in _argumentMap.Values)
            {
                argument.RegisterDependencies(
                    schemaContext.Types, reportError, parentType);
            }
        }

        internal void CompleteField(
            ISchemaContext schemaContext,
            Action<SchemaError> reportError,
            INamedType parentType)
        {
            _type = _typeFactory(schemaContext.Types);
            if (_type == null)
            {
                reportError(new SchemaError(
                    $"The type of field `{Name}` is null.",
                    parentType));
            }

            foreach (InputField argument in _argumentMap.Values)
            {
                argument.CompleteInputField(
                    schemaContext.Types, reportError, parentType);
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
                    _resolver = _resolverFactory(schemaContext.Resolvers);
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
