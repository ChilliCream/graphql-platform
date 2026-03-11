using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Info;

internal record InterfaceInfo(
    MutableInterfaceTypeDefinition InterfaceType,
    MutableSchemaDefinition Schema);
