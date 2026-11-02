using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Info;

internal record FieldArgumentInfo(
    MutableInputFieldDefinition Argument,
    MutableOutputFieldDefinition Field,
    MutableComplexTypeDefinition Type,
    MutableSchemaDefinition Schema);
