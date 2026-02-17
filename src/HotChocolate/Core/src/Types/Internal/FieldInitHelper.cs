using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Helpers;
using static HotChocolate.Utilities.ErrorHelper;

namespace HotChocolate.Internal;

public static class FieldInitHelper
{
    internal static IValueNode? CompleteDefaultValue(
        ITypeCompletionContext context,
        ArgumentConfiguration argumentDefinition,
        IInputType argumentType,
        SchemaCoordinate argumentCoordinate)
    {
        var defaultValue = argumentDefinition.DefaultValue;

        try
        {
            if (defaultValue is null && argumentDefinition.RuntimeDefaultValue is not null)
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

    public static TField[] CompleteFields<TFieldDefinition, TField>(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        IReadOnlyList<TFieldDefinition> fieldDefs,
        Func<TFieldDefinition, int, TField> fieldFactory)
        where TFieldDefinition : FieldConfiguration
        where TField : class, IFieldDefinition
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(declaringMember);
        ArgumentNullException.ThrowIfNull(fieldDefs);
        ArgumentNullException.ThrowIfNull(fieldFactory);

        return CompleteFieldsInternal(
            context,
            declaringMember,
            fieldDefs,
            fieldFactory,
            fieldDefs.Count);
    }

    public static TField[] CompleteFields<TFieldDefinition, TField>(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        IEnumerable<TFieldDefinition> fieldDefs,
        Func<TFieldDefinition, int, TField> fieldFactory,
        int maxFieldCount)
        where TFieldDefinition : FieldConfiguration
        where TField : class, IFieldDefinition
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(declaringMember);
        ArgumentNullException.ThrowIfNull(fieldDefs);
        ArgumentNullException.ThrowIfNull(fieldFactory);
        ArgumentOutOfRangeException.ThrowIfNegative(maxFieldCount);

        return CompleteFieldsInternal(
            context,
            declaringMember,
            fieldDefs,
            fieldFactory,
            maxFieldCount);
    }

    public static TField[] CompleteFields<TField>(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        TField[] fields)
        where TField : class, IFieldDefinition
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(declaringMember);
        ArgumentNullException.ThrowIfNull(fields);

        CompleteFieldsInternal(context, declaringMember, fields);

        return fields;
    }

    public static TField[] CompleteFieldsInternal<TFieldDefinition, TField>(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        IEnumerable<TFieldDefinition> fieldDefinitions,
        Func<TFieldDefinition, int, TField> fieldFactory,
        int fieldCount)
        where TFieldDefinition : FieldConfiguration
        where TField : class, IFieldDefinition
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(declaringMember);
        ArgumentNullException.ThrowIfNull(fieldDefinitions);
        ArgumentNullException.ThrowIfNull(fieldFactory);

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

        CompleteFieldsInternal(context, declaringMember, fields);

        return fields;
    }

    private static void CompleteFieldsInternal<TField>(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        TField[] fields)
        where TField : class, IFieldDefinition
    {
        if (declaringMember is IType type && fields.Length == 0)
        {
            context.ReportError(NoFields(context.Type, type));
            return;
        }

        var names = TypeMemHelper.RentNameSet();
        HashSet<string>? duplicates = null;

        foreach (var field in fields)
        {
            ((IFieldCompletion)field).CompleteField(context, declaringMember);

            if (!names.Add(field.Name))
            {
                (duplicates ??= []).Add(field.Name);
            }
        }

        TypeMemHelper.Return(names);

        if (duplicates?.Count > 0)
        {
            context.ReportError(
                DuplicateFieldName(
                    context.Type,
                    declaringMember,
                    duplicates));
        }
    }

    internal static Type CompleteRuntimeType(IType type, Type? runtimeType)
        => CompleteRuntimeType(type, runtimeType, out _);

    internal static Type CompleteRuntimeType(IType type, Type? runtimeType, out bool isOptional)
    {
        runtimeType ??= (type as IRuntimeTypeProvider)?.RuntimeType ?? typeof(object);

        if (runtimeType.IsGenericType
            && runtimeType.GetGenericTypeDefinition() == typeof(Optional<>))
        {
            isOptional = true;
            return runtimeType.GetGenericArguments()[0];
        }

        isOptional = false;
        return runtimeType;
    }
}
