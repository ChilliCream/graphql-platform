using System.Collections;
using System.Runtime.CompilerServices;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.ApolloFederation.Resolvers;

/// <summary>
/// A helper for getting field values from a representation object.
/// </summary>
internal static class ArgumentParser
{
    public static T? GetValue<T>(
        IValueNode valueNode,
        IType type,
        string[] path)
        => TryGetValue<T>(valueNode, type, path, 0, out var value) ? value : default;

    public static bool TryGetValue<T>(
        IValueNode valueNode,
        IType type,
        string[] path,
        out T? value)
        => TryGetValue(valueNode, type, path, 0, out value);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static bool TryGetValue<T>(
        IValueNode valueNode,
        IType type,
        string[] path,
        int i,
        out T? value)
    {
        type = type is NonNullType nonNullType ? nonNullType.NullableType : type;
        switch (valueNode.Kind)
        {
            case SyntaxKind.ObjectValue:
                if (path.Length <= i)
                {
                    break;
                }

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
                        if (path.Length < ++i && field.Type.IsCompositeType())
                        {
                            break;
                        }

                        return TryGetValue(fieldValue.Value, field.Type, path, i, out value);
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
                    if (!TryGetValue<object?>(itemNode, listType.ElementType, path, i, out var itemValue))
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
                if (type.NamedType() is not ScalarType scalarType)
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
                        if (path.Length >= ++i)
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
