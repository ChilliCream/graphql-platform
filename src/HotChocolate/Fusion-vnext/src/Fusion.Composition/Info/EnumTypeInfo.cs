using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Info;

internal record EnumTypeInfo(MutableEnumTypeDefinition Type, MutableSchemaDefinition Schema);
