using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.ApolloFederation.Helpers;

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
        => TryGetValue<T>(valueNode, type, path, 0, out value);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static bool TryGetValue<T>(
        IValueNode valueNode,
        IType type,
        string[] path,
        int i,
        out T? value)
    {
        switch (valueNode.Kind)
        {
            case SyntaxKind.ObjectValue:
                var current = path[i];

                if (type is not IComplexOutputType complexType ||
                    !complexType.Fields.TryGetField(current, out IOutputField? field))
                {
                    break;
                }

                foreach (ObjectFieldNode fieldValue in ((ObjectValueNode)valueNode).Fields)
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

            case SyntaxKind.NullValue:
                value = default(T);
                return true;

            case SyntaxKind.StringValue:
            case SyntaxKind.IntValue:
            case SyntaxKind.FloatValue:
            case SyntaxKind.BooleanValue:
                if (type.NamedType() is not ScalarType scalarType)
                {
                    break;
                }

                value = (T)scalarType.ParseLiteral(valueNode)!;
                return true;

            case SyntaxKind.EnumValue:
                if (type.NamedType() is not EnumType enumType)
                {
                    break;
                }

                value = (T)enumType.ParseLiteral(valueNode)!;
                return true;
        }

        value = default;
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
                var current = path[i];

                foreach (ObjectFieldNode fieldValue in ((ObjectValueNode)valueNode).Fields)
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
