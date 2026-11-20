using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace HotChocolate.Data.Projections.Expressions.Handlers;

internal static class QueryableProjectionJsonbDetector
{
    private const string DbContextTypeName = "Microsoft.EntityFrameworkCore.DbContext";
    private const string RelationalColumnType = "Relational:ColumnType";

    public static bool IsJsonbMappedProperty(
        QueryableProjectionContext context,
        PropertyInfo propertyInfo)
        => IsJsonbFromColumnAttribute(propertyInfo)
            || IsJsonbFromEntityFrameworkModel(context, propertyInfo);

    private static bool IsJsonbFromColumnAttribute(PropertyInfo propertyInfo)
        => propertyInfo
            .GetCustomAttribute<ColumnAttribute>(inherit: true)?
            .TypeName
            ?.Equals("jsonb", StringComparison.OrdinalIgnoreCase)
            ?? false;

    private static bool IsJsonbFromEntityFrameworkModel(
        QueryableProjectionContext context,
        PropertyInfo propertyInfo)
    {
        if (context.ResolverContext.Selection.Field.Member is not MethodInfo resolverMethod)
        {
            return false;
        }

        foreach (var parameter in resolverMethod.GetParameters())
        {
            if (!IsDbContextType(parameter.ParameterType))
            {
                continue;
            }

            var dbContext = context.ResolverContext.Services.GetService(parameter.ParameterType);
            if (dbContext is null)
            {
                continue;
            }

            if (TryGetColumnType(dbContext, propertyInfo, out var columnType)
                && columnType.Equals("jsonb", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDbContextType(Type type)
    {
        Type? current = type;

        while (current is not null)
        {
            if (current.FullName == DbContextTypeName)
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    private static bool TryGetColumnType(
        object dbContext,
        PropertyInfo propertyInfo,
        out string columnType)
    {
        columnType = string.Empty;

        if (propertyInfo.DeclaringType is null
            || TryGetModel(dbContext) is not { } model)
        {
            return false;
        }

        var currentType = propertyInfo.DeclaringType;
        while (currentType is not null)
        {
            if (TryGetEntityType(model, currentType) is { } entityType
                && TryGetProperty(entityType, propertyInfo) is { } efProperty
                && TryGetColumnTypeAnnotation(efProperty) is { } annotation
                && TryGetAnnotationValue(annotation) is { } annotationValue)
            {
                columnType = annotationValue;
                return true;
            }

            currentType = currentType.BaseType;
        }

        return false;
    }

    private static object? TryGetModel(object dbContext)
        => dbContext.GetType().GetProperty("Model")?.GetValue(dbContext);

    private static object? TryGetEntityType(object model, Type clrType)
        => InvokeMethod(model, "FindEntityType", [typeof(Type)], [clrType])
            ?? InvokeMethod(model, "FindEntityType", [typeof(string)], [clrType.FullName!]);

    private static object? TryGetProperty(object entityType, PropertyInfo propertyInfo)
        => InvokeMethod(entityType, "FindProperty", [typeof(string)], [propertyInfo.Name])
            ?? InvokeMethod(entityType, "FindProperty", [typeof(MemberInfo)], [propertyInfo]);

    private static object? TryGetColumnTypeAnnotation(object property)
        => InvokeMethod(property, "FindAnnotation", [typeof(string)], [RelationalColumnType]);

    private static string? TryGetAnnotationValue(object annotation)
        => annotation.GetType().GetProperty("Value")?.GetValue(annotation) as string;

    private static object? InvokeMethod(
        object instance,
        string methodName,
        Type[] parameterTypes,
        object?[] arguments)
    {
        var instanceType = instance.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var method = instanceType.GetMethod(methodName, flags, null, parameterTypes, null);

        if (method is not null)
        {
            return method.Invoke(instance, arguments);
        }

        foreach (var @interface in instanceType.GetInterfaces())
        {
            method = @interface.GetMethod(methodName, parameterTypes);
            if (method is not null)
            {
                return method.Invoke(instance, arguments);
            }
        }

        return null;
    }
}
