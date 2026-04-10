using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Info;

internal record LookupFieldInfo(
    MutableOutputFieldDefinition LookupField,
    string? Path,
    MutableSchemaDefinition Schema);
