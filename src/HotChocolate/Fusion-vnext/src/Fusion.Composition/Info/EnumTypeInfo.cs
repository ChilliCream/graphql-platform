using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Info;

internal record EnumTypeInfo(MutableEnumTypeDefinition Type, SchemaDefinition Schema);
