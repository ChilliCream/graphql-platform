#nullable enable

namespace HotChocolate.Types.Composite;

/// <summary>
/// <para>
/// By default, only a single source schema is allowed to contribute
/// a particular field to an object type.
/// </para>
///<para>
/// This prevents source schemas from inadvertently defining similarly named
/// fields that are not semantically equivalent.
/// </para>
///<para>
/// Fields must be explicitly marked as @shareable to allow multiple source
/// schemas to define them, ensuring that the decision to serve a field from
/// more than one source schema is intentional and coordinated.
/// </para>
/// <code language="graphql">
/// directive @shareable repeatable on OBJECT | FIELD_DEFINITION
/// </code>
/// <para>
/// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--shareable"/>
/// </para>
/// </summary>
[DirectiveType(
    DirectiveNames.Shareable.Name,
    DirectiveLocation.Object | DirectiveLocation.FieldDefinition,
    IsRepeatable = true)]
public sealed class Shareable
{
    private Shareable()
    {
    }

    /// <summary>
    /// The singleton instance of the <see cref="Shareable"/> directive.
    /// </summary>
    public static Shareable Instance { get; } = new();

    /// <inheritdoc />
    public override string ToString() => "@shareable";
}
