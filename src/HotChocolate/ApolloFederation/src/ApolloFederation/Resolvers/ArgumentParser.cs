using System.Collections;
using System.Reflection;
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
            {
                var current = path[i];

                if (type is not IComplexTypeDefinition complexType ||
                    !complexType.Fields.TryGetField(current, out var field))
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
            }
            case SyntaxKind.NullValue:
            {
                value = default;
                return true;
            }
            case SyntaxKind.ListValue:
            {
                if (type is not ListType listType)
                {
                    break;
                }

                var list = CreateList(listType);
                var items = ((ListValueNode)valueNode).Items;
                var flatList = !listType.ElementType.IsListType();
                var elementType = type.ElementType();
                var elementClrType = typeof(T).GetGenericArguments().Single()!;

                var innerTryGetValueMethod = typeof(ArgumentParser)
                    .GetMethod(nameof(TryGetValue), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(elementClrType);

                if (flatList)
                {
                    for (var j = 0; j < items.Count; j++)
                    {
                        var parameters = new object?[] { items[j], elementType, path, i + 1, null };
                        var succeeded = (bool) innerTryGetValueMethod.Invoke(null, parameters)!;
                        if (!succeeded)
                        {
                            throw new NotImplementedException();
                        }
                        var innerValue = parameters[4]!;
                        list.Add(innerValue);
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }

                value = (T)list;
                return true;
            }
            case SyntaxKind.StringValue:
            case SyntaxKind.IntValue:
            case SyntaxKind.FloatValue:
            case SyntaxKind.BooleanValue:
            {
                if (type.NamedType() is not ScalarType scalarType)
                {
                    break;
                }

                var literal = scalarType.ParseLiteral(valueNode)!;

                if (DefaultTypeConverter.Default.TryConvert(typeof(T), literal, out var converted))
                {
                    value = (T)converted;
                    return true;
                }

                break;
            }

            case SyntaxKind.EnumValue:
            {
                if (type.NamedType() is not EnumType enumType)
                {
                    break;
                }

                value = (T)enumType.ParseLiteral(valueNode)!;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static IList CreateList(ListType type)
        => (IList)Activator.CreateInstance(type.ToRuntimeType())!;

    public static bool Matches(IValueNode valueNode, IReadOnlyList<string[]> required)
    {
        if (required.Count == 1)
        {
            return Matches(valueNode, required[0], 0);
        }

        if (required.Count == 2)
        {
            return Matches(valueNode, required[0], 0) &&
                Matches(valueNode, required[1], 0);
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
            {
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
            }

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
