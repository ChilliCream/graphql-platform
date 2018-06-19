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
        private TypeReference _typeReference;

        internal Field(string name, Action<IFieldDescriptor> configure)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "A field name must not be null or empty.",
                    nameof(name));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            Initialize(name, configure);
        }

        internal Field(FieldDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            Initialize(descriptor);
        }

        public FieldDefinitionNode SyntaxNode { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public bool IsDeprecated { get; private set; }

        public string DeprecationReason { get; private set; }

        public IOutputType Type { get; private set; }

        public IReadOnlyDictionary<string, InputField> Arguments => _argumentMap;

        public FieldResolverDelegate Resolver { get; private set; }

        #region TypeSystemNode

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;
        IEnumerable<ITypeSystemNode> ITypeSystemNode.GetNodes()
            => _argumentMap.Values;

        #endregion

        #region Initialization

        private void Initialize(string name, Action<IFieldDescriptor> configure)
        {
            FieldDescriptor descriptor = new FieldDescriptor(null, name);
            configure(descriptor);
            Initialize(descriptor);
        }

        private void Initialize(FieldDescriptor descriptor)
        {
            if (string.IsNullOrEmpty(descriptor.Name))
            {
                throw new ArgumentException(
                    "A field name must not be null or empty.",
                    nameof(descriptor));
            }

            foreach (InputField argument in descriptor.GetArguments()
                .Select(t => new InputField(t)))
            {
                _argumentMap[argument.Name] = argument;
            }

            _member = descriptor.Member;
            _typeReference = descriptor.TypeReference;

            SyntaxNode = descriptor.SyntaxNode;
            Name = descriptor.Name;
            Description = descriptor.Description;
            DeprecationReason = descriptor.DeprecationReason;
            IsDeprecated = !string.IsNullOrEmpty(descriptor.DeprecationReason);
            Resolver = descriptor.Resolver;
        }

        internal void RegisterDependencies(
            ISchemaContext schemaContext,
            Action<SchemaError> reportError,
            INamedType parentType)
        {
            if (_typeReference != null)
            {
                schemaContext.Types.RegisterType(_typeReference);
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
            if (_typeReference != null)
            {
                Type = typeRegistry.GetType<IOutputType>(_typeReference);
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
            if (parentType is ObjectType ot && Resolver == null)
            {
                Resolver = resolverRegistry.GetResolver(parentType.Name, Name);
                if (Resolver == null)
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
