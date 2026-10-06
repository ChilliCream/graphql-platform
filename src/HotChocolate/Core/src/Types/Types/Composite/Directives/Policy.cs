namespace HotChocolate.Types.Composite;

/// <summary>
/// The @policy directive declares the policy groups of which at least one must be fully
/// satisfied to access the annotated member.
/// </summary>
[DirectiveType(
    DirectiveNames.Policy.Name,
    DirectiveLocation.FieldDefinition
    | DirectiveLocation.Object
    | DirectiveLocation.Interface
    | DirectiveLocation.Enum
    | DirectiveLocation.Scalar,
    IsRepeatable = false)]
[GraphQLDescription(
    """
    The @policy directive requires the client to satisfy all policies of at least one of
    the policy groups.
    """)]
public sealed class Policy
{
    private const string Name = $"@{DirectiveNames.Policy.Name}";

    /// <summary>
    /// Initializes a new instance of <see cref="Policy"/>.
    /// </summary>
    /// <param name="policies">
    /// The policy groups. All policies of a group are required together, and any single group
    /// is sufficient. Groups are stored in canonical order.
    /// </param>
    public Policy(IReadOnlyList<IReadOnlyList<string>> policies)
    {
        Policies = AuthorizationGroups.Canonicalize(
            DirectiveNames.Policy.Name,
            policies,
            nameof(policies));
    }

    /// <summary>
    /// Gets the policy groups in canonical order.
    /// </summary>
    [GraphQLName(DirectiveNames.Policy.Arguments.Policies)]
    [GraphQLDescription("The policy groups of which at least one must be fully satisfied.")]
    [GraphQLType<NonNullType<ListType<NonNullType<ListType<NonNullType<StringType>>>>>>]
    public IReadOnlyList<IReadOnlyList<string>> Policies { get; }

    /// <inheritdoc />
    public override string ToString()
        => $"{Name}(policies: [{string.Join(", ", Policies.Select(g => $"[{string.Join(", ", g.Select(s => $"\"{s}\""))}]"))}])";
}
