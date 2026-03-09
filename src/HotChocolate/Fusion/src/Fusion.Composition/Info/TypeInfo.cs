using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Info;

internal record TypeInfo(ITypeDefinition Type, MutableSchemaDefinition Schema);
