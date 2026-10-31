namespace HotChocolate.Types.Composite;

/// <summary>
/// The @authenticated directive declares that accessing the annotated member requires an
/// authenticated client.
/// </summary>
[DirectiveType(
    DirectiveNames.Authenticated.Name,
    DirectiveLocation.FieldDefinition
    | DirectiveLocation.Object
    | DirectiveLocation.Interface
    | DirectiveLocation.Enum
    | DirectiveLocation.Scalar,
    IsRepeatable = false)]
[GraphQLDescription("The @authenticated directive requires the client to be authenticated.")]
public sealed class Authenticated
{
    private const string Name = $"@{DirectiveNames.Authenticated.Name}";

    private Authenticated()
    {
    }

    /// <inheritdoc />
    public override string ToString() => Name;

    /// <summary>
    /// The singleton instance of the <see cref="Authenticated"/> directive.
    /// </summary>
    public static Authenticated Instance { get; } = new();
}
