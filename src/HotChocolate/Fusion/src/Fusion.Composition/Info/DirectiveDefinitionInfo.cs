using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Info;

internal record DirectiveDefinitionInfo(
    MutableDirectiveDefinition DirectiveDefinition,
    MutableSchemaDefinition Schema);
