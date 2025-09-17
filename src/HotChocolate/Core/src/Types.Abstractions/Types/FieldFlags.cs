namespace HotChocolate.Types;

/// <summary>
/// The flags provide additional semantic information about the field.
/// </summary>
[Flags]
public enum FieldFlags
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,

    /// <summary>
    /// The field is deprecated.
    /// </summary>
    Deprecated = 1 << 0,

    /// <summary>
    /// The field is used for introspection.
    /// </summary>
    Introspection = 1 << 1,

    /// <summary>
    /// Represents the __schema field.
    /// </summary>
    SchemaIntrospectionField = 1 << 2,

    /// <summary>
    /// Represents the __type field.
    /// </summary>
    TypeIntrospectionField = 1 << 3,

    /// <summary>
    /// Represents the __typename field.
    /// </summary>
    TypeNameIntrospectionField = 1 << 4,

    /// <summary>
    /// Represents a filter argument.
    /// </summary>
    FilterArgument = 1 << 5,

    /// <summary>
    /// Represents a filter operation input field.
    /// </summary>
    FilterOperation = 1 << 6,

    /// <summary>
    /// Represents a filter operation input field that is expensive.
    /// </summary>
    FilterOperationExpensive = 1 << 7,

    /// <summary>
    /// Represents a sort argument.
    /// </summary>
    SortArgument = 1 << 8,

    /// <summary>
    /// Represents a sort operation input field.
    /// </summary>
    SortOperation = 1 << 9,

    /// <summary>
    /// Represents a field that returns a relay connection.
    /// </summary>
    Connection = 1 << 10,

    /// <summary>
    /// Represents the edges field of a relay connection type.
    /// </summary>
    ConnectionEdgesField = 1 << 11,

    /// <summary>
    /// Represents the nodes field of a relay connection type.
    /// </summary>
    ConnectionNodesField = 1 << 12,

    /// <summary>
    /// Represents the first argument of a field returning a relay connection type.
    /// </summary>
    ConnectionFirstArgument = 1 << 13,

    /// <summary>
    /// Represents the last argument of a field returning a relay connection type.
    /// </summary>
    ConnectionLastArgument = 1 << 14,

    /// <summary>
    /// Represents the after argument of a field returning a relay connection type.
    /// </summary>
    ConnectionAfterArgument = 1 << 15,

    /// <summary>
    /// Represents the before argument of a field returning a relay connection type.
    /// </summary>
    ConnectionBeforeArgument = 1 << 16,

    /// <summary>
    /// Represents the global id node field.
    /// </summary>
    GlobalIdNodeField = 1 << 17,

    /// <summary>
    /// Represents the global id nodes field.
    /// </summary>
    GlobalIdNodesField = 1 << 18,

    /// <summary>
    /// Represents a collection segment (OffsetPagination).
    /// </summary>
    CollectionSegment = 1 << 19,

    /// <summary>
    /// Represents the items field of a collection segment (OffsetPagination).
    /// </summary>
    CollectionSegmentItemsField = 1 << 20,

    /// <summary>
    /// Represents the skip argument of a collection segment (OffsetPagination).
    /// </summary>
    CollectionSegmentSkipArgument = 1 << 21,

    /// <summary>
    /// Represents the take argument of a collection segment (OffsetPagination).
    /// </summary>
    CollectionSegmentTakeArgument = 1 << 22,

    /// <summary>
    /// Represents the total count field.
    /// </summary>
    TotalCount = 1 << 23
}
