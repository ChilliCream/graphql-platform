using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Info;

internal record InputFieldInfo(
    MutableInputFieldDefinition Field,
    MutableInputObjectTypeDefinition Type,
    MutableSchemaDefinition Schema);
