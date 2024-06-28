using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Represents a part of a composite type that includes the type definition and its schema.
/// </summary>
/// <param name="Type">
/// The named type that defines the structure of the composite type.
/// </param>
/// <param name="Schema">
/// The schema that describes the operations and data types supported by the composite type.
/// </param>
internal sealed record TypePart(
    INamedTypeDefinition Type,
    SchemaDefinition Schema);
