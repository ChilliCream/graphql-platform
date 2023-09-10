using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Represents an entity part that maps to an <see cref="ObjectType"/> in a <see cref="Schema"/>.
/// </summary>
/// <param name="Type">
/// The <see cref="ObjectType"/> that defines the structure of the entity.
/// </param>
/// <param name="Schema">
/// The schema to which the <see cref="ObjectType"/> belongs.
/// </param>
internal sealed record EntityPart(ObjectType Type, Schema Schema);
