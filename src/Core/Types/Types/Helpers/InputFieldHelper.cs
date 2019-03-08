using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    internal static class InputFieldHelper
    {
        public static IValueNode CreateDefaultValue(
            ICompletionContext context,
            ArgumentDefinition definition,
            IInputType fieldType)
        {
            try
            {
                if (definition.NativeDefaultValue != null)
                {
                    return fieldType.ParseValue(
                       definition.NativeDefaultValue);
                }

                return definition.DefaultValue.IsNull()
                    ? NullValueNode.Default
                    : definition.DefaultValue;
            }
            catch (Exception ex)
            {
                // TODO : RESOURCES
                context.ReportError(SchemaErrorBuilder.New()
                    .SetMessage(
                        "Could not parse the native value of input field " +
                        $"`{context.Type.Name}.{definition.Name}`.")
                    .SetCode(TypeErrorCodes.MissingType)
                    .SetTypeSystemObject(context.Type)
                    .AddSyntaxNode(definition.SyntaxNode)
                    .SetException(ex)
                    .Build());
                return NullValueNode.Default;
            }
        }
    }
}
