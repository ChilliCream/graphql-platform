using HotChocolate.Language;

namespace HotChocolate.Types;

public static class LiteralExtensions
{
    public static bool TryGetValueKind(this IValueNode literal, out ValueKind kind)
    {
        if (literal is null)
        {
            throw new ArgumentNullException(nameof(literal));
        }

        switch (literal)
        {
            case StringValueNode _:
                kind = ValueKind.String;
                return true;
            case IntValueNode _:
                kind = ValueKind.Integer;
                return true;
            case FloatValueNode _:
                kind = ValueKind.Float;
                return true;
            case BooleanValueNode _:
                kind = ValueKind.Boolean;
                return true;
            case EnumValueNode _:
                kind = ValueKind.Enum;
                return true;
            case ObjectValueNode _:
                kind = ValueKind.Object;
                return true;
            case ListValueNode _:
                kind = ValueKind.List;
                return true;
            case NullValueNode _:
                kind = ValueKind.Null;
                return true;
            case VariableNode _:
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
