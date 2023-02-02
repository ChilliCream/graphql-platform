using HotChocolate.Resolvers;

// ReSharper disable once CheckNamespace
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
            context.Selection.SyntaxNode.SelectionSet is not null)
        {
            var selections = context.GetSelections(objectType, null, true);

            for (var i = 0; i < selections.Count; i++)
            {
                if (selections[i].Field.Name is ConnectionType.Names.TotalCount)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
