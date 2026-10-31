namespace HotChocolate.Types.Composite;

/// <summary>
/// The @requiresScopes directive declares the scope groups of which at least one must be fully
/// granted to access the annotated member.
/// </summary>
[DirectiveType(
    DirectiveNames.RequiresScopes.Name,
    DirectiveLocation.FieldDefinition
    | DirectiveLocation.Object
    | DirectiveLocation.Interface
    | DirectiveLocation.Enum
    | DirectiveLocation.Scalar,
    IsRepeatable = false)]
[GraphQLDescription(
    """
    The @requiresScopes directive requires the client to be granted all scopes of at least
    one of the scope groups.
    """)]
public sealed class RequiresScopes
{
    private const string Name = $"@{DirectiveNames.RequiresScopes.Name}";

    /// <summary>
    /// Initializes a new instance of <see cref="RequiresScopes"/>.
    /// </summary>
    /// <param name="scopes">
    /// The scope groups. All scopes of a group are required together, and any single group
    /// is sufficient. Groups are stored in canonical order.
    /// </param>
    public RequiresScopes(IReadOnlyList<IReadOnlyList<string>> scopes)
    {
        Scopes = AuthorizationGroups.Canonicalize(
            DirectiveNames.RequiresScopes.Name,
            scopes,
            nameof(scopes));
    }

    /// <summary>
    /// Gets the scope groups in canonical order.
    /// </summary>
    [GraphQLName(DirectiveNames.RequiresScopes.Arguments.Scopes)]
    [GraphQLDescription("The scope groups of which at least one must be fully granted.")]
    [GraphQLType<NonNullType<ListType<NonNullType<ListType<NonNullType<StringType>>>>>>]
    public IReadOnlyList<IReadOnlyList<string>> Scopes { get; }

    /// <inheritdoc />
    public override string ToString()
        => $"{Name}(scopes: [{string.Join(", ", Scopes.Select(g => $"[{string.Join(", ", g.Select(s => $"\"{s}\""))}]"))}])";
}
