using System;
using System.Collections.Generic;

namespace HotChocolate.Language;

/// <summary>
/// The base class for named syntax nodes.
/// </summary>
public abstract class NamedSyntaxNode : INamedSyntaxNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="NamedSyntaxNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The name that this syntax node holds.
    /// </param>
    /// <param name="directives">
    /// The directives that are annotated to this syntax node.
    /// </param>
    /// <exception cref="ArgumentNullException"></exception>
    protected NamedSyntaxNode(
        Location? location,
        NameNode name,
        IReadOnlyList<DirectiveNode> directives)
    {
        Location = location;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Directives = directives ?? throw new ArgumentNullException(nameof(directives));
    }

    /// <inheritdoc />
    public abstract SyntaxKind Kind { get; }

    /// <inheritdoc />
    public Location? Location { get; }

    /// <inheritdoc />
    public NameNode Name { get; }

    /// <inheritdoc />
    public IReadOnlyList<DirectiveNode> Directives { get; }

    /// <inheritdoc />
    public abstract IEnumerable<ISyntaxNode> GetNodes();

    /// <inheritdoc />
    public abstract string ToString(bool indented);

    /// <inheritdoc cref="ISyntaxNode.ToString()" />
    public abstract override string ToString();
}
