using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using static HotChocolate.Utilities.ErrorHelper;

#nullable enable

namespace HotChocolate.Internal;

public static class FieldInitHelper
{
    internal static IValueNode? CompleteDefaultValue(
        ITypeCompletionContext context,
        ArgumentDefinition argumentDefinition,
        IInputType argumentType,
        SchemaCoordinate argumentCoordinate)
    {
        var defaultValue = argumentDefinition.DefaultValue;

        try
        {
            if(defaultValue is null && argumentDefinition.RuntimeDefaultValue is not null)
            {
                defaultValue =
                    context.DescriptorContext.InputFormatter.FormatValue(
                        argumentDefinition.RuntimeDefaultValue,
                        argumentType,
                        Path.Root);
            }

            return defaultValue;
        }
        catch (Exception ex)
        {
            context.ReportError(SchemaErrorBuilder.New()
                .SetMessage(
                    TypeResources.FieldInitHelper_InvalidDefaultValue,
                    argumentCoordinate)
                .SetCode(ErrorCodes.Schema.MissingType)
                .SetTypeSystemObject(context.Type)
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
        where TFieldDefinition : FieldDefinitionBase
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
        where TFieldDefinition : FieldDefinitionBase
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
        where TFieldDefinition : FieldDefinitionBase
        where TField : class, IField
    {
        var fieldDefs = fieldDefinitions.Where(t => !t.Ignore);

        if (context.DescriptorContext.Options.SortFieldsByName)
        {
            fieldDefs = fieldDefs.OrderBy(t => t.Name);
        }

        var index = 0;
        var fields = new TField[fieldCount];

        foreach (var fieldDefinition in fieldDefs)
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
            context.ReportError(NoFields(context.Type, type));
            return FieldCollection<TField>.Empty;
        }

        foreach (var field in fields)
        {
            ((IFieldCompletion)field).CompleteField(context, declaringMember);
        }

        var collection =  FieldCollection<TField>.TryCreate(fields, out var duplicateFieldNames);

        if (duplicateFieldNames?.Count > 0)
        {
           context.ReportError(
               DuplicateFieldName(
                   context.Type,
                   declaringMember,
                   duplicateFieldNames));
        }

        return collection;
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
