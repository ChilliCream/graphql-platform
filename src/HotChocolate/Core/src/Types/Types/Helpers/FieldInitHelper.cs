using System.Collections.Generic;
using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Configuration;
using HotChocolate.Properties;
using System.Globalization;
using System.Linq;

#nullable enable

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
                return argumentDefinition.RuntimeDefaultValue != null
                    ? context.DescriptorContext.InputFormatter.FormatLiteral(
                        argumentDefinition.RuntimeDefaultValue,
                        argumentType,
                        Path.Root)
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

        public static FieldCollection<TField> CompleteFields<TFieldDefinition, TField, TFieldType>(
            ITypeCompletionContext context,
            ITypeSystemMember declaringMember,
            IReadOnlyList<TFieldDefinition> fieldDefinitions,
            Func<TFieldDefinition, int, TField> fieldFactory)
            where TFieldDefinition : FieldDefinitionBase, IHasSyntaxNode
            where TField : FieldBase<TFieldType, TFieldDefinition>
            where TFieldType : IType

        {
            IEnumerable<TFieldDefinition> fieldDefs = fieldDefinitions.Where(t => !t.Ignore);

            if (context.DescriptorContext.Options.SortFieldsByName)
            {
                fieldDefs = fieldDefs.OrderBy(t => t.Name);
            }

            var index = 0;
            var fields = new TField[fieldDefinitions.Count];

            foreach (TFieldDefinition fieldDefinition in fieldDefs)
            {
                fields[index] = fieldFactory(fieldDefinition, index);
                index++;
            }

            if (fields.Length > index)
            {
                Array.Resize(ref fields, index);
            }

            var fieldCollection = new FieldCollection<InterfaceField>(fields);
        }

        private static void CompleteFields<TTypeDef, TFieldType, TFieldDef>(
            ITypeCompletionContext context,
            ITypeSystemMember declaringMember,
            TTypeDef definition,
            IReadOnlyCollection<FieldBase<TFieldType, TFieldDef>> fields)
            where TTypeDef : DefinitionBase
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
                    .AddSyntaxNode((type as IHasSyntaxNode)?.SyntaxNode)
                    .Build());
                return;
            }

            foreach (FieldBase<TFieldType, TFieldDef> field in fields)
            {
                field.CompleteField(context, declaringMember);
            }
        }
    }
}
