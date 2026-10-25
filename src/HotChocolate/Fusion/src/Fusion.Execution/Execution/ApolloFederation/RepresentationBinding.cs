using System.Collections.Immutable;

namespace HotChocolate.Fusion.Execution.ApolloFederation;

/// <summary>
/// Binds an operation requirement to the selection set of the representation that carries it.
/// </summary>
/// <param name="RequirementKey">The key of the bound operation requirement.</param>
/// <param name="Path">
/// The structural fields from the entity root down to the selection set that binds the
/// requirement. Empty when the requirement binds at the entity root.
/// </param>
internal readonly record struct RepresentationBinding(
    string RequirementKey,
    ImmutableArray<RepresentationPathSegment> Path);

/// <summary>
/// Describes one structural field on the representation path of an operation requirement.
/// </summary>
/// <param name="Name">The source schema field name.</param>
/// <param name="ResponseName">
/// The response name that the field's value is found under in the local composite result.
/// </param>
internal readonly record struct RepresentationPathSegment(
    string Name,
    string ResponseName);
