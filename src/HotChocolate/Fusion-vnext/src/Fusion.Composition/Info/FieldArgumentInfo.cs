using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Info;

internal record FieldArgumentInfo(
    MutableInputFieldDefinition Argument,
    OutputFieldDefinition Field,
    MutableComplexTypeDefinition Type,
    SchemaDefinition Schema);
