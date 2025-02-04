using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Info;

internal record FieldArgumentInfo(
    InputFieldDefinition Argument,
    OutputFieldDefinition Field,
    ComplexTypeDefinition Type,
    SchemaDefinition Schema);
