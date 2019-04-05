using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class Argument
        : FieldBase<IInputType, ArgumentDefinition>
        , IInputField
    {
        public Argument(
            ArgumentDefinition definition)
            : base(definition)
        {
            SyntaxNode = definition.SyntaxNode;
            DefaultValue = definition.DefaultValue;
        }

        public InputValueDefinitionNode SyntaxNode { get; }

        public IValueNode DefaultValue { get; private set; }

        protected override void OnCompleteField(
            ICompletionContext context,
            ArgumentDefinition definition)
        {
            base.OnCompleteField(context, definition);
            DefaultValue = FieldInitHelper.CreateDefaultValue(
                context, definition, Type);
        }
    }
}
