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
        private readonly Dictionary<string, InputField> _argumentMap =
            new Dictionary<string, InputField>();
        private MemberInfo _member;
        private Type _nativeNamedType;
        private ITypeNode _type;
        private FieldResolverDelegate _resolver;

        internal Field(FieldDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (string.IsNullOrEmpty(descriptor.Name))
            {
                throw new ArgumentException(
                    "A field name must not be null or empty.",
                    nameof(descriptor));
            }

            foreach (InputField argument in descriptor.Arguments
                .Select(t => new InputField(t)))
            {
                _argumentMap[argument.Name] = argument;
            }

            _member = descriptor.Member;
            _nativeNamedType = descriptor.NativeType;
            _type = descriptor.Type;

            SyntaxNode = descriptor.SyntaxNode;
            Name = descriptor.Name;
            Description = descriptor.Description;
            DeprecationReason = descriptor.DeprecationReason;
            IsDeprecated = !string.IsNullOrEmpty(descriptor.DeprecationReason);
        }

        public FieldDefinitionNode SyntaxNode { get; }

        public string Name { get; }

        public string Description { get; }

        public bool IsDeprecated { get; }

        public string DeprecationReason { get; }

        public IOutputType Type { get; private set; }

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
            if (_nativeNamedType != null)
            {
                schemaContext.Types.RegisterType(_nativeNamedType);
            }
            else if (_type != null)
            {
                schemaContext.Types.RegisterType(_type);
            }

            if (_member != null)
            {
                schemaContext.Resolvers.RegisterResolver(
                    new MemberResolverBinding(parentType.Name, Name, _member));
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
            CompleteType(schemaContext.Types, reportError, parentType);
            CompleteResolver(schemaContext.Resolvers, reportError, parentType);

            foreach (InputField argument in _argumentMap.Values)
            {
                argument.CompleteInputField(
                    schemaContext.Types, reportError, parentType);
            }
        }

        private void CompleteType(
            ITypeRegistry typeRegistry,
            Action<SchemaError> reportError,
            INamedType parentType)
        {

            if (_nativeNamedType != null)
            {
                Type = typeRegistry.GetType<IOutputType>(_nativeNamedType);
            }
            else if (_type != null)
            {
                Type = typeRegistry.GetOutputType(_type);
            }

            if (Type == null)
            {
                reportError(new SchemaError(
                    $"The type of field `{parentType.Name}.{Name}` is null.",
                    parentType));
            }
        }

        private void CompleteResolver(
            IResolverRegistry resolverRegistry,
            Action<SchemaError> reportError,
            INamedType parentType)
        {
            if (parentType is ObjectType ot)
            {
                _resolver = resolverRegistry.GetResolver(parentType.Name, Name);
                if (_resolver == null)
                {
                    reportError(new SchemaError(
                        $"The field `{parentType.Name}.{Name}` " +
                        "has no resolver.", parentType));
                }
            }
        }

        #endregion
    }
}
