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

        public Type ClrType { get; private set; }

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
            context.RegisterDependencyRange(
                definition.GetDependencies(),
                TypeDependencyKind.Default);
        }

        protected override void OnCompleteType(
            ICompletionContext context,
            InterfaceTypeDefinition definition)
        {
            SyntaxNode = definition.SyntaxNode;
            ClrType = definition.ClrType;
            Fields = new FieldCollection<InterfaceField>(
                definition.Fields.Select(t => new InterfaceField(t)));

            CompleteFields(context);
            CompleteAbstractTypeResolver(
                context,
                definition.ResolveAbstractType);
        }

        private void CompleteFields(
            ICompletionContext context)
        {
            foreach (InterfaceField field in Fields)
            {
                field.CompleteField(context);
            }

            if (Fields.Count == 0)
            {
                // TODO : RESOURCES
                context.ReportError(SchemaErrorBuilder.New()
                    .SetMessage($"Interface `{Name}` has no fields declared.")
                    .SetCode(TypeErrorCodes.MissingType)
                    .SetTypeSystemObject(context.Type)
                    .AddSyntaxNode(SyntaxNode)
                    .Build());
            }
        }

        private void Initialize(Action<IInputObjectTypeDescriptor> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            InputObjectTypeDescriptor descriptor = CreateDescriptor();
            configure(descriptor);

            InputObjectTypeDescription description =
                descriptor.CreateDescription();
            ClrType = description.ClrType;
            SyntaxNode = description.SyntaxNode;
            Fields = new FieldCollection<InputField>(
                description.Fields.Select(t => new InputField(t)));

            Initialize(description.Name, description.Description,
                new DirectiveCollection(this,
                    DirectiveLocation.InputObject,
                    description.Directives));
        }

        protected override void OnRegisterDependencies(
            ITypeInitializationContext context)
        {
            base.OnRegisterDependencies(context);

            foreach (INeedsInitialization field in Fields
                .Cast<INeedsInitialization>())
            {
                field.RegisterDependencies(context);
            }
        }

        protected override void OnCompleteType(
            ITypeInitializationContext context)
        {
            ITypeConversion converter = context.Services.GetTypeConversion();

            _objectToValueConverter =
                new InputObjectToObjectValueConverter(converter);
            _valueToObjectConverter =
                new ObjectValueToInputObjectConverter(converter);

            base.OnCompleteType(context);

            CompleteClrType(context);
            CompleteFields(context);
        }

        private void CompleteClrType(
            ITypeInitializationContext context)
        {
            if (ClrType == null
                && context.TryGetNativeType(this, out Type clrType))
            {
                ClrType = clrType;
            }

            if (ClrType == null)
            {
                ClrType = typeof(object);
            }
        }

        private void CompleteFields(
            ITypeInitializationContext context)
        {
            foreach (INeedsInitialization field in Fields
                .Cast<INeedsInitialization>())
            {
                field.CompleteType(context);
            }

            if (Fields.IsEmpty)
            {
                context.ReportError(new SchemaError(string.Format(
                    CultureInfo.InvariantCulture,
                    TypeResources.InputObjectType_NoFields,
                    Name)));
            }
        }

        protected override InputObjectTypeDefinition CreateDefinition(IInitializationContext context)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
