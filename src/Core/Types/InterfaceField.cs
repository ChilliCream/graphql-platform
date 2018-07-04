using System;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class InterfaceField
        : IOutputField
    {
        private TypeReference _typeReference;

        internal InterfaceField(Action<IInterfaceFieldDescriptor> configure)
            : this(() => ExecuteConfigure(configure))
        {
        }

        internal InterfaceField(Func<InterfaceFieldDescription> descriptionFactory)
            : this(ExecuteFactory(descriptionFactory))
        {
        }

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

        private static InterfaceFieldDescription ExecuteConfigure(
            Action<IInterfaceFieldDescriptor> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            InterfaceFieldDescriptor descriptor = new InterfaceFieldDescriptor();
            configure(descriptor);
            return descriptor.CreateFieldDescription();
        }

        internal static T ExecuteFactory<T>(
            Func<T> descriptionFactory)
            where T : InterfaceFieldDescription
        {
            if (descriptionFactory == null)
            {
                throw new ArgumentNullException(nameof(descriptionFactory));
            }

            return descriptionFactory();
        }

        public FieldDefinitionNode SyntaxNode { get; }

        public string Name { get; }

        public INamedType DeclaringType { get; private set; }

        public IOutputType Type { get; private set; }

        public IFieldCollection<InputField> Arguments { get; }

        IFieldCollection<IInputField> IOutputField.Arguments => Arguments;

        public bool IsDeprecated { get; }

        public string DeprecationReason { get; }

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
}
