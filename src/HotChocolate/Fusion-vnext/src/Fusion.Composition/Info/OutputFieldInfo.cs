using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Info;

internal record OutputFieldInfo(
    OutputFieldDefinition Field,
    ComplexTypeDefinition Type,
    SchemaDefinition Schema);
