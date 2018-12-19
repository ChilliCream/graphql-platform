using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class ObjectFieldBase
        : FieldBase<IOutputType>
        , IOutputField
    {
        internal ObjectFieldBase(
            ObjectFieldDescriptionBase fieldDescription)
            : base(fieldDescription, DirectiveLocation.FieldDefinition)
        {
            SyntaxNode = fieldDescription.SyntaxNode;
            Arguments = new FieldCollection<InputField>(
                fieldDescription.Arguments.Select(t => new InputField(t)));
            IsDeprecated = !string.IsNullOrEmpty(
                fieldDescription.DeprecationReason);
            DeprecationReason = fieldDescription.DeprecationReason;
        }

        public FieldDefinitionNode SyntaxNode { get; }

        public FieldCollection<InputField> Arguments { get; }

        IFieldCollection<IInputField> IOutputField.Arguments => Arguments;

        public virtual bool IsIntrospectionField { get; } = false;

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
