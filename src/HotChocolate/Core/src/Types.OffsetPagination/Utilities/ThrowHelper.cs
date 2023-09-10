using HotChocolate.Types;

namespace HotChocolate.Utilities;

internal static class ThrowHelper
{
    public static GraphQLException OffsetPagingHandler_MaxPageSize(
        int requestedItems,
        int maxAllowedItems,
        IObjectField field,
        Path path)
        => new GraphQLException(
            ErrorBuilder.New()
                .SetMessage("The maximum allowed items per page were exceeded.")
                .SetCode(ErrorCodes.Paging.MaxPaginationItems)
                .SetPath(path)
                .SetSyntaxNode(field.SyntaxNode)
                .SetExtension(nameof(field), field.Coordinate.ToString())
                .SetExtension(nameof(requestedItems), requestedItems)
                .SetExtension(nameof(maxAllowedItems), maxAllowedItems)
                .Build());

    public static GraphQLException OffsetPagingHandler_NoBoundariesSet(
        IObjectField field,
        Path path)
        => new GraphQLException(
            ErrorBuilder.New()
                .SetMessage(
                    "You must provide take to properly paginate the `{0}`.",
                    field.Type.NamedType().Name)
                .SetCode(ErrorCodes.Paging.NoPagingBoundaries)
                .SetPath(path)
                .SetSyntaxNode(field.SyntaxNode)
                .SetExtension(nameof(field), field.Coordinate.ToString())
                .Build());
}
