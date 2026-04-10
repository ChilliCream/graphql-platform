using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Info;

internal record ObjectFieldInfo(
    MutableOutputFieldDefinition Field,
    MutableObjectTypeDefinition Type,
    MutableSchemaDefinition Schema);
