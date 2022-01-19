using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.ApolloFederation;

internal static class ArgumentParser
{
    public static bool TryGetValue<T>(
        IValueNode valueNode,
        IType type,
        string[] path,
        out T? value)
        => TryGetValue(valueNode, type, path, out value);

    private static bool TryGetValue<T>(
        IValueNode valueNode,
        IType type,
        string[] path,
        int i,
        out T? value)
    {
        var current = path[i];

        switch (valueNode.Kind)
        {
            case SyntaxKind.ObjectValue:
                if (type is not IComplexOutputType complexType ||
                    !complexType.Fields.TryGetField(current, out IOutputField? field))
                {
                    break;
                }

                foreach (ObjectFieldNode fieldValue in ((ObjectValueNode)valueNode).Fields)
                {
                    if (field.Name.Value.EqualsOrdinal(current))
                    {
                        if (path.Length > ++i)
                        {
                            return TryGetValue(fieldValue.Value, field.Type, path, i, out value);
                        }
                        break;
                    }
                }
                break;

            case SyntaxKind.StringValue:
            case SyntaxKind.IntValue:
            case SyntaxKind.FloatValue:
            case SyntaxKind.BooleanValue:
                if (path.Length == ++i)
                {
                    if (type.NamedType() is not ScalarType scalarType)
                    {
                        break;
                    }

                    value = (T)scalarType.ParseValue(valueNode);
                    return true;
                }
                break;

            case SyntaxKind.EnumValue:
                if (path.Length == ++i)
                {
                    if (type.NamedType() is not EnumType enumType)
                    {
                        break;
                    }

                    value = (T)enumType.ParseValue(valueNode);
                    return true;
                }
                break;
        }

        value = default;
        return false;
    }
}
