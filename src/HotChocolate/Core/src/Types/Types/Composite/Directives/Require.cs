#nullable enable

using HotChocolate.Fusion.Language;

namespace HotChocolate.Types.Composite;

/// <summary>
/// <para>
/// The @require directive is used to express data requirements with other source schemas.
/// Arguments annotated with the @require directive are removed from the composite schema
/// and the value for these will be resolved by the distributed executor.
/// </para>
/// <para>
/// directive @require(field: FieldSelectionMap!) on ARGUMENT_DEFINITION
/// </para>
/// <para>
/// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--require"/>
/// </para>
/// </summary>
[DirectiveType(
    DirectiveNames.Require.Name,
    DirectiveLocation.ArgumentDefinition,
    IsRepeatable = false)]
public sealed class Require
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Require"/> class.
    /// </summary>
    /// <param name="field">The field selection map.</param>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="field"/> is <c>null</c>.
    /// </exception>
    public Require(IValueSelectionNode field)
    {
        ArgumentNullException.ThrowIfNull(field);
        Field = field;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Require"/> class.
    /// </summary>
    /// <param name="field">The field selection map.</param>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="field"/> is <c>null</c>.
    /// </exception>
    public Require(string field)
    {
        ArgumentNullException.ThrowIfNull(field);
        Field = FieldSelectionMapParser.Parse(field);
    }

    /// <summary>
    /// Gets the field selection map.
    /// </summary>
    [GraphQLName(DirectiveNames.Require.Arguments.Field)]
    [GraphQLDescription("The field selection map syntax.")]
    [GraphQLType<NonNullType<FieldSelectionMapType>>]
    public IValueSelectionNode Field { get; }

    /// <inheritdoc />
    public override string ToString() => $"@require(field: \"{Field.ToString(false)}\")";
}
