using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Info;

internal record InputTypeInfo(InputObjectTypeDefinition InputType, MutableSchemaDefinition Schema);
