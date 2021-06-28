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
            ITypeCompletionContext context,
            ArgumentDefinition argumentDefinition,
            IInputType argumentType,
            FieldCoordinate argumentCoordinate)
        {
            try
            {
                return argumentDefinition.NativeDefaultValue != null
                    ? argumentType.ParseValue(argumentDefinition.NativeDefaultValue)
                    : argumentDefinition.DefaultValue;
            }
            catch (Exception ex)
            {
                context.ReportError(SchemaErrorBuilder.New()
                    .SetMessage(
                        TypeResources.FieldInitHelper_InvalidDefaultValue,
                        argumentCoordinate)
                    .SetCode(ErrorCodes.Schema.MissingType)
                    .SetTypeSystemObject(context.Type)
                    .AddSyntaxNode(argumentDefinition.SyntaxNode)
                    .SetException(ex)
                    .Build());
                return NullValueNode.Default;
            }
        }

        public static void CompleteFields<TTypeDef, TFieldType, TFieldDef>(
            ITypeCompletionContext context,
            TTypeDef definition,
            IReadOnlyCollection<FieldBase<TFieldType, TFieldDef>> fields)
            where TTypeDef : DefinitionBase, IHasSyntaxNode
            where TFieldType : IType
            where TFieldDef : FieldDefinitionBase, IHasSyntaxNode
        {
            if (context.Type is IType type && fields.Count == 0)
            {
                context.ReportError(SchemaErrorBuilder.New()
                    .SetMessage(string.Format(
                        CultureInfo.InvariantCulture,
                        TypeResources.FieldInitHelper_NoFields,
                        type.Kind.ToString(),
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
