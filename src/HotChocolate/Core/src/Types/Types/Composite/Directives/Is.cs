#nullable enable

using HotChocolate.Fusion.Language;

namespace HotChocolate.Types.Composite;

/// <summary>
/// <para>
/// The @is directive is utilized on lookup fields to describe how the arguments
/// can be mapped from the entity type that the lookup field resolves.
/// </para>
/// <para>
/// The mapping establishes semantic equivalence between disparate type system members
/// across source schemas and is used in cases where an argument does not directly align
/// with a field on the entity type.
/// </para>
/// <para>
/// directive @is(field: FieldSelectionMap!) on ARGUMENT_DEFINITION
/// </para>
/// <para>
/// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--is"/>
/// </para>
/// </summary>
[DirectiveType(
    DirectiveNames.Is.Name,
    DirectiveLocation.ArgumentDefinition,
    IsRepeatable = false)]
[GraphQLDescription(
    """
    The @is directive is utilized on lookup fields to describe how the arguments
    can be mapped from the entity type that the lookup field resolves.
    """)]
public sealed class Is
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Is"/> class.
    /// </summary>
    /// <param name="field">The field selection map.</param>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="field"/> is <c>null</c>.
    /// </exception>
    public Is(IValueSelectionNode field)
    {
        ArgumentNullException.ThrowIfNull(field);
        Field = field;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Is"/> class.
    /// </summary>
    /// <param name="field">The field selection map.</param>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="field"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="FieldSelectionMapSyntaxException">
    /// The syntax used in the <paramref name="field"/> parameter is invalid.
    /// </exception>
    public Is(string field)
    {
        ArgumentNullException.ThrowIfNull(field);
        Field = FieldSelectionMapParser.Parse(field);
    }

    /// <summary>
    /// Gets the field selection map.
    /// </summary>
    [GraphQLType<NonNullType<FieldSelectionMapType>>]
    [GraphQLDescription("The field selection map syntax.")]
    public IValueSelectionNode Field { get; }

    /// <inheritdoc />
    public override string ToString() => $"@is(field: \"{Field.ToString(false)}\")";
}
