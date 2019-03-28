using System.Collections.Generic;
using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Configuration;

namespace HotChocolate.Types
{
    internal static class FieldInitHelper
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

        public static void CompleteFields<TTypeDef, TFieldType, TFieldDef>(
            ICompletionContext context,
            TTypeDef definition,
            IReadOnlyCollection<FieldBase<TFieldType, TFieldDef>> fields)
            where TTypeDef : DefinitionBase, IHasSyntaxNode
            where TFieldType : IType
            where TFieldDef : FieldDefinitionBase

        {
            if (fields.Count == 0)
            {
                // TODO : RESOURCES
                context.ReportError(SchemaErrorBuilder.New()
                    .SetMessage($"Interface `{definition.Name}` has no fields declared.")
                    .SetCode(TypeErrorCodes.MissingType)
                    .SetTypeSystemObject(context.Type)
                    .AddSyntaxNode(definition.SyntaxNode)
                    .Build());
                return;
            }

            foreach (FieldBase<TFieldType, TFieldDef> field in fields)
            {
                field.CompleteField(context);
            }
        }
    }
}
