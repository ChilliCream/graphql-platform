using HotChocolate.Types;
using static HotChocolate.Types.Properties.CursorResources;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Utilities;

internal static class ThrowHelper
{
    public static GraphQLException PagingHandler_MaxPageSize(
        int requestedItems,
        int maxAllowedItems,
        IObjectField field,
        Path path)
        => new GraphQLException(
            ErrorBuilder.New()
                .SetMessage(ThrowHelper_PagingHandler_MaxPageSize)
                .SetCode(ErrorCodes.Paging.MaxPaginationItems)
                .SetPath(path)
                .SetSyntaxNode(field.SyntaxNode)
                .SetExtension(nameof(field), field.Coordinate.ToString())
                .SetExtension(nameof(requestedItems), requestedItems)
                .SetExtension(nameof(maxAllowedItems), maxAllowedItems)
                .Build());

    public static GraphQLException PagingHandler_NoBoundariesSet(
        IObjectField field,
        Path path)
        => new GraphQLException(
            ErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_PagingHandler_NoBoundariesSet,
                    field.Type.TypeName())
                .SetCode(ErrorCodes.Paging.NoPagingBoundaries)
                .SetPath(path)
                .SetSyntaxNode(field.SyntaxNode)
                .SetExtension(nameof(field), field.Coordinate.ToString())
                .Build());

    public static SchemaException PagingObjectFieldDescriptorExtensions_InvalidType()
        => new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(PagingObjectFieldDescriptorExtensions_SchemaTypeNotValid)
                .SetCode(ErrorCodes.Paging.SchemaTypeInvalid)
                .Build());
}
