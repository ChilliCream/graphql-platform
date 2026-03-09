using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Info;

internal record OutputFieldInfo(
    MutableOutputFieldDefinition Field,
    MutableComplexTypeDefinition Type,
    MutableSchemaDefinition Schema);
