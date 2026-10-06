using System.Collections.Immutable;

namespace HotChocolate.Fusion.Features;

/// <summary>
/// A merged member that is protected by inheritance.
/// </summary>
/// <param name="Coordinate">The protected member.</param>
/// <param name="Paths">
/// The paths from the members that contribute the requirement to the protected member.
/// </param>
/// <param name="SourceSchemas">The source schemas that annotated the contributing members.</param>
internal sealed record InheritedAuthorization(
    SchemaCoordinate Coordinate,
    ImmutableArray<string> Paths,
    ImmutableArray<string> SourceSchemas);
