using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// <para>Represents a fragment spread.</para>
/// <para>FragmentSpread : ... FragmentName Arguments? Directives?</para>
/// </summary>
public sealed class FragmentSpreadNode : NamedSyntaxNode, ISelectionNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="FragmentSpreadNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The name of the fragment that is spread.
    /// </param>
    /// <param name="directives">
    /// The applied directives.
    /// </param>
    [Obsolete("Use the constructor overload that accepts arguments.")]
    public FragmentSpreadNode(
        Location? location,
        NameNode name,
        IReadOnlyList<DirectiveNode> directives)
        : this(location, name, [], directives)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FragmentSpreadNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The name of the fragment that is spread.
    /// </param>
    /// <param name="arguments">
    /// The values passed to the variables that the spread fragment declares.
    /// </param>
    /// <param name="directives">
    /// The applied directives.
    /// </param>
    public FragmentSpreadNode(
        Location? location,
        NameNode name,
        IReadOnlyList<ArgumentNode> arguments,
        IReadOnlyList<DirectiveNode> directives)
        : base(location, name, directives)
    {
        Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
    }

    /// <inheritdoc/>
    public override SyntaxKind Kind => SyntaxKind.FragmentSpread;

    /// <summary>
    /// Gets the values passed to the variables that the spread fragment declares.
    /// </summary>
    public IReadOnlyList<ArgumentNode> Arguments { get; }

    /// <inheritdoc/>
    public override IEnumerable<ISyntaxNode> GetNodes()
    {
        yield return Name;

        foreach (var argument in Arguments)
        {
            yield return argument;
        }

        foreach (var directive in Directives)
        {
            yield return directive;
        }
    }

    /// <summary>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </summary>
    /// <returns>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </returns>
    public override string ToString() => SyntaxPrinter.Print(this, true);

    /// <summary>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </summary>
    /// <param name="indented">
    /// A value that indicates whether the GraphQL output should be formatted,
    /// which includes indenting nested GraphQL tokens, adding
    /// new lines, and adding white space between property names and values.
    /// </param>
    /// <returns>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </returns>
    public override string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

    public FragmentSpreadNode WithLocation(Location? location)
        => new(location, Name, Arguments, Directives);

    public FragmentSpreadNode WithName(NameNode name) => new(Location, name, Arguments, Directives);

    public FragmentSpreadNode WithArguments(IReadOnlyList<ArgumentNode> arguments)
        => new(Location, Name, arguments, Directives);

    public FragmentSpreadNode WithDirectives(IReadOnlyList<DirectiveNode> directives)
        => new(Location, Name, Arguments, directives);
}
