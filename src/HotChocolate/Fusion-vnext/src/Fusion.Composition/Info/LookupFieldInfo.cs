using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Info;

internal record LookupFieldInfo(
    OutputFieldDefinition LookupField,
    string? Path,
    SchemaDefinition Schema);
