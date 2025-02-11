using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Info;

internal record InputFieldInfo(
    MutableInputFieldDefinition Field,
    InputObjectTypeDefinition Type,
    SchemaDefinition Schema);
