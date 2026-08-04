using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static System.Reflection.BindingFlags;

namespace HotChocolate.ApolloFederation.Resolvers;

/// <summary>
/// A helper for getting field values from a representation object.
/// </summary>
internal static class ArgumentParser
{
    public static T? GetValue<T>(
        IValueNode valueNode,
        IType type,
        string[] path,
        Schema schema)
        => TryGetValue<T>(valueNode, type, path, 0, schema, out var value) ? value : default;

    public static bool TryGetValue<T>(
        IValueNode valueNode,
        IType type,
        string[] path,
        Schema schema,
        out T? value)
        => TryGetValue(valueNode, type, path, 0, schema, out value);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static bool TryGetValue<T>(
        IValueNode valueNode,
        IType type,
        string[] path,
        int i,
        Schema schema,
        out T? value)
    {
        type = type is NonNullType nonNullType ? nonNullType.NullableType : type;

        if (path.Length <= i)
        {
            if (TryConvertValue(valueNode, type, typeof(T), schema, out var converted))
            {
                value = (T?)converted;
                return true;
            }

            value = default;
            return false;
        }

        switch (valueNode.Kind)
        {
            case SyntaxKind.ObjectValue:
                var current = path[i];

                if (type is not IComplexTypeDefinition complexType
                    || !complexType.Fields.TryGetField(current, out var field))
                {
                    break;
                }

                foreach (var fieldValue in ((ObjectValueNode)valueNode).Fields)
                {
                    if (fieldValue.Name.Value.EqualsOrdinal(current))
                    {
                        return TryGetValue(fieldValue.Value, field.Type, path, i + 1, schema, out value);
                    }
                }
                break;

            case SyntaxKind.ListValue:
                if (type is not ListType listType
                    || path.Length > i
                    || !TryGetListElementType(typeof(T), out var elementType))
                {
                    break;
                }

                var itemType = typeof(List<>).MakeGenericType(elementType);
                var items = (IList)Activator.CreateInstance(itemType)!;
                var listValue = (ListValueNode)valueNode;

                foreach (var itemNode in listValue.Items)
                {
                    if (!TryGetValue<object?>(
                        itemNode,
                        listType.ElementType,
                        path,
                        i,
                        schema,
                        out var itemValue))
                    {
                        value = default;
                        return false;
                    }

                    if (itemValue is null)
                    {
                        items.Add(null);
                        continue;
                    }

                    if (elementType.IsInstanceOfType(itemValue))
                    {
                        items.Add(itemValue);
                        continue;
                    }

                    if (DefaultTypeConverter.Default.TryConvert(
                        elementType,
                        itemValue,
                        out var convertedItem))
                    {
                        items.Add(convertedItem);
                        continue;
                    }

                    value = default;
                    return false;
                }

                if (items is T castedItems)
                {
                    value = castedItems;
                    return true;
                }

                if (DefaultTypeConverter.Default.TryConvert(
                    typeof(T),
                    items,
                    out var convertedList))
                {
                    value = (T)convertedList;
                    return true;
                }

                break;

            case SyntaxKind.NullValue:
                value = default;
                return true;

            case SyntaxKind.StringValue:
            case SyntaxKind.IntValue:
            case SyntaxKind.FloatValue:
            case SyntaxKind.BooleanValue:
                var namedType = type.NamedType();

                if (namedType is EnumType stringEnumType
                    && valueNode is StringValueNode stringValue
                    && stringEnumType.TryGetRuntimeValue(stringValue.Value, out var enumValue))
                {
                    if (enumValue is T castedEnum)
                    {
                        value = castedEnum;
                        return true;
                    }

                    if (DefaultTypeConverter.Default.TryConvert(typeof(T), enumValue, out var convertedEnum))
                    {
                        value = (T)convertedEnum;
                        return true;
                    }
                }

                if (namedType is not ScalarType scalarType)
                {
                    break;
                }

                var literal = scalarType.CoerceInputLiteral(valueNode)!;

                if (DefaultTypeConverter.Default.TryConvert(typeof(T), literal, out var converted))
                {
                    value = (T)converted;
                    return true;
                }

                break;

            case SyntaxKind.EnumValue:
                if (type.NamedType() is not EnumType enumType)
                {
                    break;
                }

                value = (T)enumType.CoerceInputLiteral(valueNode)!;
                return true;
        }

        value = default;
        return false;
    }

    private static bool TryConvertValue(
        IValueNode valueNode,
        IType type,
        Type targetType,
        Schema schema,
        out object? value)
    {
        type = type is NonNullType nonNullType ? nonNullType.NullableType : type;

        switch (valueNode.Kind)
        {
            case SyntaxKind.NullValue:
                value = null;
                return true;

            case SyntaxKind.ObjectValue:
                return TryConvertObjectValue(
                    (ObjectValueNode)valueNode,
                    type,
                    targetType,
                    schema,
                    out value);

            case SyntaxKind.ListValue:
                return TryConvertListValue((ListValueNode)valueNode, type, targetType, schema, out value);

            case SyntaxKind.StringValue:
            case SyntaxKind.IntValue:
            case SyntaxKind.FloatValue:
            case SyntaxKind.BooleanValue:
                return TryConvertLeafValue(valueNode, type, targetType, out value);

            case SyntaxKind.EnumValue:
                if (type.NamedType() is EnumType enumType)
                {
                    value = enumType.CoerceInputLiteral(valueNode);
                    return TryConvertIfNeeded(targetType, ref value);
                }
                break;
        }

        value = default;
        return false;
    }

    private static bool TryConvertObjectValue(
        ObjectValueNode valueNode,
        IType type,
        Type targetType,
        Schema schema,
        out object? value)
    {
        if (type.NamedType() is not IComplexTypeDefinition complexType)
        {
            value = default;
            return false;
        }

        var runtimeType = ResolveRuntimeType(complexType, targetType);

        // When the declared field type is an abstract interface, the runtime type cannot
        // be instantiated directly. If the representation carries a __typename discriminator
        // we resolve the concrete object type and reconstruct that type instead.
        if ((runtimeType == typeof(object) || runtimeType.IsAbstract || runtimeType.IsInterface)
            && complexType is IInterfaceTypeDefinition interfaceType
            && TryResolveConcreteType(valueNode, interfaceType, schema, out var concreteType))
        {
            complexType = concreteType;
            runtimeType = concreteType.RuntimeType;
        }

        if (runtimeType == typeof(object)
            || runtimeType.IsAbstract
            || runtimeType.IsInterface
            || Activator.CreateInstance(runtimeType) is not { } instance)
        {
            value = default;
            return false;
        }

        foreach (var fieldValue in valueNode.Fields)
        {
            var fieldName = fieldValue.Name.Value;

            if (!complexType.Fields.TryGetField(fieldName, out var field)
                || TryGetProperty(runtimeType, field, fieldName) is not { } property)
            {
                continue;
            }

            if (!TryConvertValue(
                fieldValue.Value,
                field.Type,
                property.PropertyType,
                schema,
                out var propertyValue))
            {
                value = default;
                return false;
            }

            property.SetValue(instance, propertyValue);
        }

        value = instance;
        return TryConvertIfNeeded(targetType, ref value);
    }

    private static bool TryConvertListValue(
        ListValueNode valueNode,
        IType type,
        Type targetType,
        Schema schema,
        out object? value)
    {
        if (type is not ListType listType
            || !TryGetListElementType(targetType, out var elementType))
        {
            value = default;
            return false;
        }

        var itemType = typeof(List<>).MakeGenericType(elementType);
        var items = (IList)Activator.CreateInstance(itemType)!;

        foreach (var itemNode in valueNode.Items)
        {
            if (!TryConvertValue(itemNode, listType.ElementType, elementType, schema, out var itemValue))
            {
                value = default;
                return false;
            }

            items.Add(itemValue);
        }

        value = items;
        return TryConvertIfNeeded(targetType, ref value);
    }

    private static bool TryConvertLeafValue(
        IValueNode valueNode,
        IType type,
        Type targetType,
        out object? value)
    {
        var namedType = type.NamedType();

        if (namedType is EnumType stringEnumType
            && valueNode is StringValueNode stringValue
            && stringEnumType.TryGetRuntimeValue(stringValue.Value, out var enumValue))
        {
            value = enumValue;
            return TryConvertIfNeeded(targetType, ref value);
        }

        if (namedType is not ScalarType scalarType)
        {
            value = default;
            return false;
        }

        value = scalarType.CoerceInputLiteral(valueNode);
        return TryConvertIfNeeded(targetType, ref value);
    }

    private static bool TryConvertIfNeeded(Type targetType, ref object? value)
    {
        if (value is null || targetType.IsInstanceOfType(value))
        {
            return true;
        }

        if (DefaultTypeConverter.Default.TryConvert(targetType, value, out var converted))
        {
            value = converted;
            return true;
        }

        return false;
    }

    private static Type ResolveRuntimeType(ITypeDefinition type, Type targetType)
    {
        if (targetType != typeof(object))
        {
            return targetType;
        }

        return type.RuntimeType;
    }

    private static bool TryResolveConcreteType(
        ObjectValueNode valueNode,
        IInterfaceTypeDefinition interfaceType,
        Schema schema,
        [NotNullWhen(true)] out ObjectType? concreteType)
    {
        foreach (var fieldValue in valueNode.Fields)
        {
            if (fieldValue.Name.Value.EqualsOrdinal(IntrospectionFieldNames.TypeName)
                && fieldValue.Value is StringValueNode typeName)
            {
                if (schema.Types.TryGetType<ObjectType>(typeName.Value, out var objectType)
                    && objectType.IsImplementing(interfaceType))
                {
                    concreteType = objectType;
                    return true;
                }

                concreteType = null;
                return false;
            }
        }

        concreteType = null;
        return false;
    }

    private static PropertyInfo? TryGetProperty(
        Type runtimeType,
        IOutputFieldDefinition field,
        string fieldName)
    {
        if (field is ObjectField { Member: PropertyInfo { SetMethod: not null } memberProperty })
        {
            return memberProperty;
        }

        foreach (var candidateProperty in runtimeType.GetProperties(Instance | Public))
        {
            if (candidateProperty.SetMethod is not null
                && candidateProperty.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
            {
                return candidateProperty;
            }
        }

        return null;
    }

    private static bool TryGetListElementType(Type type, out Type elementType)
    {
        if (type.IsArray)
        {
            elementType = type.GetElementType()!;
            return true;
        }

        if (type.IsGenericType
            && type.GetGenericArguments() is [var genericElement]
            && type.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
        {
            elementType = genericElement;
            return true;
        }

        var enumerableType = type
            .GetInterfaces()
            .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerableType is not null)
        {
            elementType = enumerableType.GetGenericArguments()[0];
            return true;
        }

        elementType = null!;
        return false;
    }

    public static bool Matches(IValueNode valueNode, IReadOnlyList<string[]> required)
    {
        if (required.Count == 1)
        {
            return Matches(valueNode, required[0], 0);
        }

        if (required.Count == 2)
        {
            return Matches(valueNode, required[0], 0)
                && Matches(valueNode, required[1], 0);
        }

        for (var i = 0; i < required.Count; i++)
        {
            if (!Matches(valueNode, required[i], 0))
            {
                return false;
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static bool Matches(IValueNode valueNode, string[] path, int i)
    {
        switch (valueNode.Kind)
        {
            case SyntaxKind.ObjectValue:
                var current = path[i];

                foreach (var fieldValue in ((ObjectValueNode)valueNode).Fields)
                {
                    if (fieldValue.Name.Value.EqualsOrdinal(current))
                    {
                        if (path.Length == ++i)
                        {
                            return true;
                        }

                        if (path.Length > i)
                        {
                            return Matches(fieldValue.Value, path, i);
                        }
                        break;
                    }
                }
                break;

            case SyntaxKind.NullValue:
            case SyntaxKind.StringValue:
            case SyntaxKind.IntValue:
            case SyntaxKind.FloatValue:
            case SyntaxKind.BooleanValue:
            case SyntaxKind.EnumValue:
                return true;
        }

        return false;
    }
}
