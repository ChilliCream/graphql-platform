using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Info;

internal record EnumValueInfo(
    MutableEnumValue EnumValue,
    MutableEnumTypeDefinition EnumType,
    MutableSchemaDefinition Schema);
