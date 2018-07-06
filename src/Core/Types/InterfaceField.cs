using System;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class InterfaceField
        : FieldBase<IOutputType>
        , IOutputField
    {
        internal InterfaceField(Action<IInterfaceFieldDescriptor> configure)
            : this(() => ExecuteConfigure(configure))
        {
        }

        internal InterfaceField(Func<InterfaceFieldDescription> descriptionFactory)
            : this(DescriptorHelpers.ExecuteFactory(descriptionFactory))
        {
        }

        internal InterfaceField(InterfaceFieldDescription fieldDescription)
            : base(fieldDescription)
        {
            SyntaxNode = fieldDescription.SyntaxNode;
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

            var descriptor = new InterfaceFieldDescriptor();
            configure(descriptor);
            return descriptor.CreateDescription();
        }

        public FieldDefinitionNode SyntaxNode { get; }

        public FieldCollection<InputField> Arguments { get; }

        IFieldCollection<IInputField> IOutputField.Arguments => Arguments;

        public bool IsDeprecated { get; }

        public string DeprecationReason { get; }

        protected override void OnRegisterDependencies(
            ITypeInitializationContext context)
        {
            base.OnRegisterDependencies(context);

            foreach (INeedsInitialization argument in Arguments
                .Cast<INeedsInitialization>())
            {
                argument.RegisterDependencies(context);
            }
        }

        protected override void OnCompleteType(
            ITypeInitializationContext context)
        {
            base.OnCompleteType(context);

            foreach (INeedsInitialization argument in Arguments
                .Cast<INeedsInitialization>())
            {
                argument.CompleteType(context);
            }
        }
    }
}
