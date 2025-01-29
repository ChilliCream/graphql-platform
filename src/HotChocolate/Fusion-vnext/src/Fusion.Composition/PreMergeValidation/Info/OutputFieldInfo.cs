using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.PreMergeValidation.Info;

internal record OutputFieldInfo(
    OutputFieldDefinition Field,
    INamedTypeDefinition Type,
    SchemaDefinition Schema);
