using HotChocolate.Types;
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
                    field.Type.TypeName())
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
                    field.Type.TypeName())
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

    public static SchemaException PagingObjectFieldDescriptorExtensions_ConnectionNameConflict(
        string connectionTypeName,
        string existingNodeType,
        bool existingIncludeTotalCount,
        bool existingIncludeNodesField,
        string currentNodeType,
        bool currentIncludeTotalCount,
        bool currentIncludeNodesField)
        => new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    "The connection type `{0}` is inferred for multiple fields with "
                    + "conflicting paging settings. Existing: nodeType `{1}`, "
                    + "includeTotalCount `{2}`, includeNodesField `{3}`. Current: "
                    + "nodeType `{4}`, includeTotalCount `{5}`, includeNodesField `{6}`. "
                    + "Set an explicit ConnectionName to disambiguate.",
                    connectionTypeName,
                    existingNodeType,
                    existingIncludeTotalCount,
                    existingIncludeNodesField,
                    currentNodeType,
                    currentIncludeTotalCount,
                    currentIncludeNodesField)
                .SetCode(ErrorCodes.Schema.DuplicateTypeName)
                .Build());

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
