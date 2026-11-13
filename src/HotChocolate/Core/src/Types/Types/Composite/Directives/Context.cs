namespace HotChocolate.Types.Composite;

/// <summary>
/// The @context directive declares that every value of the annotated type sets the named
/// context. A @require field selection map in the same source schema reads from the context
/// with a context path such as <c>$name.field</c>.
/// </summary>
[DirectiveType(
    DirectiveNames.Context.Name,
    DirectiveLocation.Object
    | DirectiveLocation.Interface
    | DirectiveLocation.Union,
    IsRepeatable = true)]
[GraphQLDescription(
    """
    The @context directive declares that every value of the annotated type sets the named
    context. A @require field selection map in the same source schema reads from the context
    with a context path such as $name.field.
    """)]
public sealed class Context
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Context"/> class.
    /// </summary>
    /// <param name="name">The name of the context.</param>
    /// <exception cref="ArgumentException">
    /// The <paramref name="name"/> is <c>null</c> or empty.
    /// </exception>
    public Context(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        Name = name;
    }

    /// <summary>
    /// Gets the name of the context.
    /// </summary>
    [GraphQLName(DirectiveNames.Context.Arguments.Name)]
    [GraphQLDescription("The name of the context.")]
    public string Name { get; }

    /// <inheritdoc />
    public override string ToString()
        => $"@context(name: \"{Name}\")";
}
