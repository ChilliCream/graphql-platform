using HotChocolate.Resolvers;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types.Pagination;

internal static class OffsetPaginationResolverContextExtensions
{
    /// <summary>
    /// <para>
    /// TotalCount is one of the heaviest operations. It is only necessary to load totalCount
    /// when it is enabled (IncludeTotalCount) and when it is contained in the selection set.
    /// </para>
    /// <para>This method checks if the total count is selected</para>
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static bool IsTotalCountSelected(this IResolverContext context)
    {
        // TotalCount is one of the heaviest operations. It is only necessary to load totalCount
        // when it is enabled (IncludeTotalCount) and when it is contained in the selection set.
        if (context.Selection is { Type: ObjectType objectType, IsLeaf: false })
        {
            var selectionSet = context.Selection.DeclaringOperation.GetSelectionSet(context.Selection, objectType);

            foreach (var selection in selectionSet.Selections)
            {
                if (selection.Field.Name is OffsetPagingFieldNames.TotalCount)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
