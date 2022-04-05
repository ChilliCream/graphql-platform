using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;

#nullable enable

namespace HotChocolate.Internal;

public static class FieldInitHelper
{
    internal static IValueNode? CompleteDefaultValue(
        ITypeCompletionContext context,
        ArgumentDefinition argumentDefinition,
        IInputType argumentType,
        FieldCoordinate argumentCoordinate)
    {
        try
        {
            return argumentDefinition.RuntimeDefaultValue != null
                ? context.DescriptorContext.InputFormatter.FormatValue(
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
        IReadOnlyList<TFieldDefinition> fieldDefs,
        Func<TFieldDefinition, int, TField> fieldFactory)
        where TFieldDefinition : FieldDefinitionBase, IHasSyntaxNode
        where TField : class, IField
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (declaringMember is null)
        {
            throw new ArgumentNullException(nameof(declaringMember));
        }

        if (fieldDefs is null)
        {
            throw new ArgumentNullException(nameof(fieldDefs));
        }

        if (fieldFactory is null)
        {
            throw new ArgumentNullException(nameof(fieldFactory));
        }

        return CompleteFieldsInternal(
            context,
            declaringMember,
            fieldDefs,
            fieldFactory,
            fieldDefs.Count);
    }

    public static FieldCollection<TField> CompleteFields<TFieldDefinition, TField>(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        IEnumerable<TFieldDefinition> fieldDefs,
        Func<TFieldDefinition, int, TField> fieldFactory,
        int maxFieldCount)
        where TFieldDefinition : FieldDefinitionBase, IHasSyntaxNode
        where TField : class, IField
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (declaringMember is null)
        {
            throw new ArgumentNullException(nameof(declaringMember));
        }

        if (fieldDefs is null)
        {
            throw new ArgumentNullException(nameof(fieldDefs));
        }

        if (fieldFactory is null)
        {
            throw new ArgumentNullException(nameof(fieldDefs));
        }

        if (maxFieldCount < 1)
        {
            throw new ArgumentOutOfRangeException(
                paramName: nameof(maxFieldCount),
                actualValue: maxFieldCount,
                message: TypeResources.FieldInitHelper_CompleteFields_MaxFieldCountToSmall);
        }

        return CompleteFieldsInternal(
            context,
            declaringMember,
            fieldDefs,
            fieldFactory,
            maxFieldCount);
    }

    public static FieldCollection<TField> CompleteFields<TField>(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        TField[] fields)
        where TField : class, IField
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (declaringMember is null)
        {
            throw new ArgumentNullException(nameof(declaringMember));
        }

        if (fields is null)
        {
            throw new ArgumentNullException(nameof(fields));
        }

        return CompleteFieldsInternal(context, declaringMember, fields);
    }

    public static FieldCollection<TField> CompleteFieldsInternal<TFieldDefinition, TField>(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        IEnumerable<TFieldDefinition> fieldDefinitions,
        Func<TFieldDefinition, int, TField> fieldFactory,
        int fieldCount)
        where TFieldDefinition : FieldDefinitionBase, IHasSyntaxNode
        where TField : class, IField
    {
        IEnumerable<TFieldDefinition> fieldDefs = fieldDefinitions.Where(t => !t.Ignore);

        if (context.DescriptorContext.Options.SortFieldsByName)
        {
            fieldDefs = fieldDefs.OrderBy(t => t.Name);
        }

        var index = 0;
        var fields = new TField[fieldCount];

        foreach (TFieldDefinition fieldDefinition in fieldDefs)
        {
            fields[index] = fieldFactory(fieldDefinition, index);
            index++;
        }

        if (fields.Length > index)
        {
            Array.Resize(ref fields, index);
        }

        return CompleteFieldsInternal(context, declaringMember, fields);
    }

    private static FieldCollection<TField> CompleteFieldsInternal<TField>(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        TField[] fields)
        where TField : class, IField
    {
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
            ((IFieldCompletion)field).CompleteField(context, declaringMember);
        }

        return new FieldCollection<TField>(fields);
    }

    internal static Type CompleteRuntimeType(IType type, Type? runtimeType)
        => CompleteRuntimeType(type, runtimeType, out _);

    internal static Type CompleteRuntimeType(IType type, Type? runtimeType, out bool isOptional)
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
