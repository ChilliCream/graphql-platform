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
    public class InterfaceField
        : IOutputField
    {
        private TypeReference _typeReference;

        internal InterfaceField(InterfaceFieldDescription fieldDescription)
        {
            if (fieldDescription == null)
            {
                throw new ArgumentNullException(nameof(fieldDescription));
            }

            _typeReference = fieldDescription.TypeReference;

            SyntaxNode = fieldDescription.SyntaxNode;
            Name = fieldDescription.Name;
            Arguments = new FieldCollection<InputField>(
                fieldDescription.Arguments.Select(t => new InputField(t)));
            IsDeprecated = !string.IsNullOrEmpty(fieldDescription.DeprecationReason);
            DeprecationReason = fieldDescription.DeprecationReason;
        }

        public FieldDefinitionNode SyntaxNode { get; private set; }

        public string Name { get; private set; }

        public INamedType DeclaringType { get; private set; }

        public IOutputType Type { get; private set; }

        public IFieldCollection<InputField> Arguments { get; private set; }

        IFieldCollection<IInputField> IOutputField.Arguments => Arguments;

        public bool IsDeprecated { get; private set; }

        public string DeprecationReason { get; private set; }

        protected bool IsCompleted { get; private set; }

        protected void Complete()
        {
            IsCompleted = true;
        }

        internal void RegisterDependencies(
            ISchemaContext schemaContext,
            Action<SchemaError> reportError,
            INamedType parentType)
        {
            if (!IsCompleted)
            {
                OnRegisterDependencies(schemaContext, reportError, parentType);
            }
        }

        internal virtual void OnRegisterDependencies(
            ISchemaContext schemaContext,
            Action<SchemaError> reportError,
            INamedType parentType)
        {
            if (_typeReference != null)
            {
                schemaContext.Types.RegisterType(_typeReference);
            }

            foreach (InputField argument in Arguments)
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
            if (!IsCompleted)
            {
                OnCompleteField(schemaContext, reportError, parentType);
                Complete();
            }
        }

        internal virtual void OnCompleteField(
            ISchemaContext schemaContext,
            Action<SchemaError> reportError,
            INamedType parentType)
        {
            DeclaringType = parentType;
            Type = this.ResolveFieldType<IOutputType>(
                schemaContext.Types,
                reportError, _typeReference);

            foreach (InputField argument in Arguments)
            {
                argument.CompleteInputField(
                    schemaContext.Types, reportError, parentType);
            }
        }
    }


    public class Field
        : IOutputField
    {
        private readonly Dictionary<string, InputField> _argumentMap =
            new Dictionary<string, InputField>();
        private MemberInfo _member;
        private TypeReference _typeReference;
        private bool _completed;

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

        public INamedType DeclaringType { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public bool IsDeprecated { get; private set; }

        public string DeprecationReason { get; private set; }

        public IOutputType Type { get; private set; }

        public IReadOnlyDictionary<string, InputField> Arguments => _argumentMap;

        IReadOnlyDictionary<string, IInputField> IOutputField.Arguments
            => _argumentMap.ToDictionary(t => t.Key, t => t.Value);

        public FieldResolverDelegate Resolver { get; private set; }

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
            if (!_completed)
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
        }

        internal void CompleteField(
            ISchemaContext schemaContext,
            Action<SchemaError> reportError,
            INamedType parentType)
        {
            if (!_completed)
            {
                DeclaringType = parentType;
                Type = this.ResolveFieldType<IOutputType>(schemaContext.Types,
                    reportError, _typeReference);

                CompleteResolver(schemaContext.Resolvers, reportError, parentType);

                foreach (InputField argument in _argumentMap.Values)
                {
                    argument.CompleteInputField(
                        schemaContext.Types, reportError, parentType);
                }

                _completed = true;
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
