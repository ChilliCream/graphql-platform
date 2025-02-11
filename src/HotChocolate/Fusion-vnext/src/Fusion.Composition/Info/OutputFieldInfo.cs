using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Info;

internal record OutputFieldInfo(
    OutputFieldDefinition Field,
    MutableComplexTypeDefinition Type,
    SchemaDefinition Schema);
