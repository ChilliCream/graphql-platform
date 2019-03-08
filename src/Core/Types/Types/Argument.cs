using System;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class Argument
        : FieldBase<IInputType, ArgumentDefinition>
        , IInputField
        , IHasClrType
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

        private PropertyInfo Property { get; set; }

        public Type ClrType => Property?.PropertyType ?? typeof(object);

        protected override void OnCompleteField(
            ICompletionContext context,
            ArgumentDefinition definition)
        {
            base.OnCompleteField(context, definition);
            CompleteDefaultValue(context, definition);
        }

        #region Initialization

        private void CompleteDefaultValue(
            ICompletionContext context,
            ArgumentDefinition definition)
        {
            try
            {
                if (DefaultValue == null)
                {
                    if (definition.NativeDefaultValue == null)
                    {
                        DefaultValue = NullValueNode.Default;
                    }
                    else
                    {
                        DefaultValue = Type.ParseValue(
                            definition.NativeDefaultValue);
                    }
                }
            }
            catch (Exception ex)
            {
                // TODO : RESOURCES
                context.ReportError(SchemaErrorBuilder.New()
                    .SetMessage(
                        "Could not parse the native value of input field " +
                        $"`{context.Type.Name}.{Name}`.")
                    .SetCode(TypeErrorCodes.MissingType)
                    .SetTypeSystemObject(context.Type)
                    .AddSyntaxNode(SyntaxNode)
                    .SetException(ex)
                    .Build());
            }
        }

        #endregion
    }
}
