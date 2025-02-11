using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Info;

internal record EnumValueInfo(
    MutableEnumValue MutableEnumValue,
    MutableEnumTypeDefinition MutableEnumType,
    SchemaDefinition Schema);
