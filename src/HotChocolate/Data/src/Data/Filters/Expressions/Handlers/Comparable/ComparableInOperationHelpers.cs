using HotChocolate.Internal;
using HotChocolate.Language;

namespace HotChocolate.Data.Filters.Expressions;

internal static class ComparableInOperationHelpers
{
    public static bool IsValueNull(
        IExtendedType runtimeType,
        IValueNode node,
        object? parsedValue)
    {
        if (parsedValue is null)
        {
            return true;
        }

        if (!runtimeType.IsNullable && node is ListValueNode values)
        {
            for (var i = 0; i < values.Items.Count; i++)
            {
                if (values.Items[i].Kind == SyntaxKind.NullValue)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
