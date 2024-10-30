using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Represents an entity part that maps to an <see cref="ObjectTypeDefinition"/> in a <see cref="Schema"/>.
/// </summary>
/// <param name="Type">
/// The <see cref="ObjectTypeDefinition"/> that defines the structure of the entity.
/// </param>
/// <param name="Schema">
/// The schema to which the <see cref="ObjectTypeDefinition"/> belongs.
/// </param>
internal sealed record EntityPart(ObjectTypeDefinition Type, SchemaDefinition Schema);
