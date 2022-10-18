using System.Collections.Generic;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Pagination;

internal static class CursorPaginationResolverContextExtensions
{
    /// <summary>
    /// TotalCount is one of the heaviest operations. It is only necessary to load totalCount
    /// when it is enabled (IncludeTotalCount) and when it is contained in the selection set.
    ///
    /// This method checks if the total count is selected
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static bool IsTotalCountSelected(this IResolverContext context)
    {
        // TotalCount is one of the heaviest operations. It is only necessary to load totalCount
        // when it is enabled (IncludeTotalCount) and when it is contained in the selection set.
        if (context.Selection.Type is ObjectType objectType &&
            context.Selection.SyntaxNode.SelectionSet is { } selectionSet)
        {
            IReadOnlyList<IFieldSelection> selections =
                context.GetSelections(objectType, selectionSet, true);

            for (var i = 0; i < selections.Count; i++)
            {
                if (selections[i].Field.Name.Value is OffsetPagingFieldNames.TotalCount)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool IsItemsFieldSelected(this IResolverContext context)
    {
        // Items is one of the heaviest operations. It is only necessary to load items when it is contained in the selection set.
        if (context.Selection.Type is ObjectType objectType &&
            context.Selection.SyntaxNode.SelectionSet is { } selectionSet)
        {
            IReadOnlyList<IFieldSelection> selections =
                context.GetSelections(objectType, selectionSet, true);

            for (var i = 0; i < selections.Count; i++)
            {
                if (selections[i].Field.Name.Value is OffsetPagingFieldNames.Items)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
