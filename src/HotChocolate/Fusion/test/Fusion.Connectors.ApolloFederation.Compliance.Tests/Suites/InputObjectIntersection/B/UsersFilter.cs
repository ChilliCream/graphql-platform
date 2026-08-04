namespace HotChocolate.Fusion.Suites.InputObjectIntersection.B;

/// <summary>
/// The <c>UsersFilter</c> input object in the <c>b</c> subgraph: declares
/// both <c>offset</c> and <c>first</c>. Composition takes the
/// intersection of input fields, so only <c>first</c> is exposed.
/// </summary>
public sealed record UsersFilter(int First, int? Offset);
