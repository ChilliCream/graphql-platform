using System.Collections.Generic;
using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Configuration;
using HotChocolate.Properties;
using System.Globalization;

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
                context.ReportError(SchemaErrorBuilder.New()
                    .SetMessage(TypeResources.FieldInitHelper_InvalidDefaultValue)
                    .SetCode(ErrorCodes.Schema.MissingType)
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
            if (context.Type is IType type && fields.Count == 0)
            {
                string kind = context.Type is IType t
                    ? t.Kind.ToString()
                    : TypeKind.Directive.ToString();

                context.ReportError(SchemaErrorBuilder.New()
                    .SetMessage(string.Format(
                        CultureInfo.InvariantCulture,
                        TypeResources.FieldInitHelper_NoFields,
                        kind,
                        context.Type.Name))
                    .SetCode(ErrorCodes.Schema.MissingType)
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
