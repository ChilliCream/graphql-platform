using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Info;

internal record TypeInfo(INamedTypeDefinition Type, SchemaDefinition Schema);
