using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.PreMergeValidation.Info;

internal record TypeInfo(INamedTypeDefinition Type, SchemaDefinition Schema);
