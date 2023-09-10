using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Internal;

/// <summary>
/// Common helpers for 'in' and 'nin' filter operations
/// </summary>
public static class ValueNullabilityHelpers
{
    /// <summary>
    /// Validates if the provided array is valid
    /// </summary>
    public static bool IsListValueValid(
        IType type,
        IExtendedType runtimeType,
        IValueNode node)
    {
        if (type.IsListType() &&
            !runtimeType.IsNullable &&
            node is ListValueNode values)
        {
            for (var i = 0; i < values.Items.Count; i++)
            {
                if (values.Items[i].Kind == SyntaxKind.NullValue)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
