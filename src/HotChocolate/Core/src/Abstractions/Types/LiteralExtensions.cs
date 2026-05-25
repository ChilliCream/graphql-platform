using HotChocolate.Language;

namespace HotChocolate.Types;

public static class LiteralExtensions
{
    public static bool TryGetValueKind(this IValueNode literal, out ValueKind kind)
    {
        ArgumentNullException.ThrowIfNull(literal);

        switch (literal)
        {
            case StringValueNode:
                kind = ValueKind.String;
                return true;
            case IntValueNode:
                kind = ValueKind.Integer;
                return true;
            case FloatValueNode:
                kind = ValueKind.Float;
                return true;
            case BooleanValueNode:
                kind = ValueKind.Boolean;
                return true;
            case EnumValueNode:
                kind = ValueKind.Enum;
                return true;
            case ObjectValueNode:
                kind = ValueKind.Object;
                return true;
            case ListValueNode:
                kind = ValueKind.List;
                return true;
            case NullValueNode:
                kind = ValueKind.Null;
                return true;
            case VariableNode:
                kind = ValueKind.Unknown;
                return true;
            default:
                kind = default;
                return false;
        }
    }

    public static ValueKind GetValueKind(this IValueNode literal)
    {
        if (TryGetValueKind(literal, out var kind))
        {
            return kind;
        }

        throw new InvalidOperationException(
            "The specified value node does not represent a value literal.");
    }
}
