using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Info;

internal record InputFieldInfo(
    InputFieldDefinition Field,
    InputObjectTypeDefinition Type,
    SchemaDefinition Schema);
