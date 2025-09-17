namespace HotChocolate.Types.Descriptors.Configurations;

[Flags]
internal enum CoreFieldFlags
{
    None = 0,

    Deprecated = 1 << 0,
    Introspection = 1 << 1,
    SchemaIntrospectionField = 1 << 2,
    TypeIntrospectionField = 1 << 3,
    TypeNameIntrospectionField = 1 << 4,

    FilterArgument = 1 << 5,
    FilterOperationField = 1 << 6,
    FilterExpensiveOperationField = 1 << 7,

    SortArgument = 1 << 8,
    SortOperationField = 1 << 9,

    Connection = 1 << 10,
    ConnectionEdgesField = 1 << 11,
    ConnectionNodesField = 1 << 12,
    ConnectionFirstArgument = 1 << 13,
    ConnectionLastArgument = 1 << 14,
    ConnectionAfterArgument = 1 << 15,
    ConnectionBeforeArgument = 1 << 16,

    GlobalIdNodeField = 1 << 17,
    GlobalIdNodesField = 1 << 18,

    CollectionSegment = 1 << 19,
    CollectionSegmentItemsField = 1 << 20,
    CollectionSegmentSkipArgument = 1 << 21,
    CollectionSegmentTakeArgument = 1 << 22,

    TotalCount = 1 << 23,

    // the following flags are only internal and not part of the public API
    Ignored = 1 << 24,
    ParallelExecutable = 1 << 25,
    Stream = 1 << 26,
    Sealed = 1 << 27,
    SourceGenerator = 1 << 28,
    MutationQueryField = 1 << 29,
    WithRequirements = 1 << 30,
    UsesProjections = 1 << 31
}
