using System;
using System.Globalization;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Types
{
    public class InputObjectType
        : NamedTypeBase<InputObjectTypeDefinition>
        , INamedInputType
    {
        private readonly Action<IInputObjectTypeDescriptor> _configure;
        private InputObjectToObjectValueConverter _objectToValueConverter;
        private ObjectValueToInputObjectConverter _valueToObjectConverter;

        internal InputObjectType()
        {
            _configure = Configure;
        }

        internal InputObjectType(Action<IInputObjectTypeDescriptor> configure)
        {
            _configure = configure
                ?? throw new ArgumentNullException(nameof(configure));
        }

        public override TypeKind Kind => TypeKind.InputObject;

        public InputObjectTypeDefinitionNode SyntaxNode { get; private set; }

        public FieldCollection<InputField> Fields { get; private set; }

        #region IInputType

        public bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            return literal is ObjectValueNode
                || literal is NullValueNode;
        }

        public object ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is ObjectValueNode ov)
            {
                return _valueToObjectConverter.Convert(ov, this);
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            throw new ArgumentException(
                TypeResources.InputObjectType_CannotParseLiteral,
                nameof(literal));
        }

        public bool IsInstanceOfType(object value)
        {
            if (value is null)
            {
                return true;
            }

            return ClrType.IsInstanceOfType(value);
        }

        public IValueNode ParseValue(object value)
        {
            return _objectToValueConverter.Convert(this, value);
        }

        #endregion

        #region Initialization

        protected override InputObjectTypeDefinition CreateDefinition(
            IInitializationContext context)
        {
            InputObjectTypeDescriptor descriptor =
                InputObjectTypeDescriptor.New(
                    DescriptorContext.Create(context.Services),
                    GetType());
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IInputObjectTypeDescriptor descriptor)
        {
        }

        protected override void OnRegisterDependencies(
            IInitializationContext context,
            InputObjectTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);

            context.RegisterDependencyRange(
                definition.GetDependencies(),
                TypeDependencyKind.Default);
        }

        protected override void OnCompleteType(
            ICompletionContext context,
            InputObjectTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            SyntaxNode = definition.SyntaxNode;
            Fields = new FieldCollection<InputField>(
                definition.Fields.Select(t => new InputField(t)));

            FieldInitHelper.CompleteFields(context, definition, Fields);
        }

        #endregion
    }
}
