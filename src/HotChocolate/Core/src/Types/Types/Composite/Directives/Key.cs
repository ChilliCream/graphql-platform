#nullable enable

using HotChocolate.Language;

namespace HotChocolate.Types.Composite;

/// <summary>
/// <para>
/// The @key directive is used to designate an entity's unique key,
/// which identifies how to uniquely reference an instance of
/// an entity across different source schemas.
/// </para>
/// <para>
/// directive @key(fields: FieldSelectionSet!) on OBJECT | INTERFACE
/// </para>
/// <para>
/// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--key"/>
/// </para>
/// </summary>
[DirectiveType(
    DirectiveNames.Key.Name,
    DirectiveLocation.Object
    | DirectiveLocation.Interface,
    IsRepeatable = false)]
[GraphQLDescription(
    """
    The @key directive is used to designate an entity's unique key,
    which identifies how to uniquely reference an instance of
    an entity across different source schemas.
    """)]
public sealed class Key
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Key"/> class.
    /// </summary>
    /// <param name="fields">
    /// The fields that are used to identify an entity.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="fields"/> is <c>null</c>.
    /// </exception>
    public Key(SelectionSetNode fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        Fields = fields;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Key"/> class.
    /// </summary>
    /// <param name="fields">
    /// The fields that are used to identify an entity.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="fields"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="SyntaxException">
    /// The syntax used in the <paramref name="fields"/> parameter is invalid.
    /// </exception>
    public Key(string fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        fields = $"{{ {fields.Trim('{', '}')} }}";
        Fields = Utf8GraphQLParser.Syntax.ParseSelectionSet(fields);
    }

    /// <summary>
    /// A selection set that represents the fields that make up the unique key for an entity.
    /// </summary>
    [GraphQLType<NonNullType<FieldSelectionSetType>>]
    [GraphQLDescription("The field selection set syntax.")]
    public SelectionSetNode Fields { get; }

    /// <inheritdoc />
    public override string ToString()
        => $"@key(fields: {Fields.ToString(false)[1..^1]})";
}
