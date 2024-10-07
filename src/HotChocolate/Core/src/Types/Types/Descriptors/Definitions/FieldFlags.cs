#nullable enable
namespace HotChocolate.Types.Descriptors.Definitions;

[Flags]
internal enum FieldFlags
{
    None = 0,
    Introspection = 2,
    Deprecated = 4,
    Ignored = 8,
    ParallelExecutable = 16,
    Stream = 32,
    Sealed = 64,
    TypeNameField = 128,
    FilterArgument = 256,
    FilterOperationField = 512,
    FilterExpensiveOperationField = 1024,
    SortArgument = 2048,
    SortOperationField = 4096,
    Connection = 8192,
    CollectionSegment = 16384,
    SkipArgument = 32768,
    TotalCount = 65536,
    SourceGenerator = 131072,
    MutationQueryField = 262144,
    ConnectionEdgesField = 524288,
    ConnectionNodesField = 1048576,
    ItemsField = 2097152,
    WithRequirements = 4194304,
    UsesProjections = 8388608,
    GlobalIdNodeField = 16777216,
    GlobalIdNodesField = 33554432,
}
