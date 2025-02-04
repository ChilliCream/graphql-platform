using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Info;

internal record EnumValueInfo(
    EnumValue EnumValue,
    EnumTypeDefinition EnumType,
    SchemaDefinition Schema);
