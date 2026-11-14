using IOutputFieldDefinition = HotChocolate.Types.IOutputFieldDefinition;
using PagingErrorHelper = HotChocolate.Types.Pagination.ErrorHelper;
using PagingTypeExtensions = HotChocolate.Types.TypeExtensions;
using static HotChocolate.Types.Properties.CursorResources;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Utilities;

internal static class ThrowHelper
{
    public static GraphQLException PagingHandler_MinPageSize(
        int requestedItems,
        IOutputFieldDefinition field,
        Path path)
        => new GraphQLException(
            ErrorBuilder.New()
                .SetMessage(ThrowHelper_PagingHandler_MinPageSize)
                .SetCode(ErrorCodes.Paging.MinPaginationItems)
                .SetPath(path)
                .SetCoordinate(field.Coordinate)
                .SetExtension(nameof(requestedItems), requestedItems)
                .Build());

    public static GraphQLException PagingHandler_MaxPageSize(
        int requestedItems,
        int maxAllowedItems,
        IOutputFieldDefinition field,
        Path path)
        => new GraphQLException(
            ErrorBuilder.New()
                .SetMessage(ThrowHelper_PagingHandler_MaxPageSize)
                .SetCode(ErrorCodes.Paging.MaxPaginationItems)
                .SetPath(path)
                .SetCoordinate(field.Coordinate)
                .SetExtension(nameof(requestedItems), requestedItems)
                .SetExtension(nameof(maxAllowedItems), maxAllowedItems)
                .Build());

    public static GraphQLException PagingHandler_NoBoundariesSet(
        IOutputFieldDefinition field,
        Path path)
        => new GraphQLException(
            ErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_PagingHandler_NoBoundariesSet,
                    PagingTypeExtensions.TypeName(field.Type))
                .SetCode(ErrorCodes.Paging.NoPagingBoundaries)
                .SetPath(path)
                .SetCoordinate(field.Coordinate)
                .Build());

    public static GraphQLException PagingHandler_FirstValueNotSet(
        IOutputFieldDefinition field,
        Path path)
        => new GraphQLException(
            ErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_PagingHandler_FirstValueNotSet,
                    PagingTypeExtensions.TypeName(field.Type))
                .SetCode(ErrorCodes.Paging.FirstValueNotSet)
                .SetPath(path)
                .SetCoordinate(field.Coordinate)
                .Build());

    public static SchemaException PagingObjectFieldDescriptorExtensions_InvalidType()
        => new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(PagingObjectFieldDescriptorExtensions_SchemaTypeNotValid)
                .SetCode(ErrorCodes.Paging.SchemaTypeInvalid)
                .Build());

    public static GraphQLException PagingHandler_RelativeCursorsNotEnabled(
        IOutputFieldDefinition field,
        Path path)
        => new(PagingErrorHelper.RelativeCursorsNotEnabled(field, path));

    public static GraphQLException InvalidIndexCursor(string argument, string cursor)
        => new GraphQLException(
            ErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_InvalidIndexCursor_Message,
                    argument)
                .SetExtension("argument", argument)
                .SetExtension("cursor", cursor)
                .SetCode(ErrorCodes.Paging.InvalidCursor)
                .Build());
}
