using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Info;

internal record InputTypeInfo(
    MutableInputObjectTypeDefinition InputType,
    MutableSchemaDefinition Schema);
