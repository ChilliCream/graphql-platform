using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.PreMergeValidation.Info;

internal record FieldArgumentInfo(
    InputFieldDefinition Argument,
    OutputFieldDefinition Field,
    INamedTypeDefinition Type,
    SchemaDefinition Schema);
