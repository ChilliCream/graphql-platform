using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.PreMergeValidation.Info;

internal record InputFieldInfo(
    InputFieldDefinition Field,
    INamedTypeDefinition Type,
    SchemaDefinition Schema);
