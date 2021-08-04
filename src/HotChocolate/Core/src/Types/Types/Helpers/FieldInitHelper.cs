using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Descriptors.Helpers;

#nullable enable

namespace HotChocolate.Types.Helpers
{
    internal static class FieldInitHelper
    {
        public static IValueNode? CompleteDefaultValue(
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

        public static FieldCollection<TField> CompleteFields<TFieldDefinition, TField>(
            ITypeCompletionContext context,
            ITypeSystemMember declaringMember,
            IReadOnlyList<TFieldDefinition> fieldDefinitions,
            Func<TFieldDefinition, int, TField> fieldFactory)
            where TFieldDefinition : FieldDefinitionBase, IHasSyntaxNode
            where TField : class, IField, IFieldCompletion
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

            if (declaringMember is IType type && fields.Length == 0)
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
                return FieldCollection<TField>.Empty;
            }

            foreach (TField field in fields)
            {
                field.CompleteField(context, declaringMember);
            }

            return new FieldCollection<TField>(fields);
        }

        public static Type CompleteRuntimeType(IType type, Type? runtimeType)
            => CompleteRuntimeType(type, runtimeType, out _);

        public static Type CompleteRuntimeType(IType type, Type? runtimeType, out bool isOptional)
        {
            runtimeType ??= (type as IHasRuntimeType)?.RuntimeType ?? typeof(object);

            if (runtimeType.IsGenericType &&
                runtimeType.GetGenericTypeDefinition() == typeof(Optional<>))
            {
                isOptional = true;
                return runtimeType.GetGenericArguments()[0];
            }

            isOptional = false;
            return runtimeType;
        }
    }
}
