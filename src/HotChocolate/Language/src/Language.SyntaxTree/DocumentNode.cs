using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// <para>
/// The <see cref="DocumentNode"/> represents a parsed GraphQL document
/// which also is the root node of a parsed GraphQL document.
/// </para>
/// <para>The document can contain schema definition nodes or query nodes.</para>
/// </summary>
public sealed class DocumentNode : ISyntaxNode
{
    private int _count = -1;
    private int _fieldsCount = -1;

    /// <summary>
    /// Initializes a new instance of <see cref="DocumentNode"/>.
    /// </summary>
    /// <param name="definitions">
    /// The GraphQL definitions this document contains.
    /// </param>
    public DocumentNode(
        IReadOnlyList<IDefinitionNode> definitions)
        : this(null, definitions) { }

    /// <summary>
    /// Initializes a new instance of <see cref="DocumentNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the document in the parsed source text.
    /// </param>
    /// <param name="definitions">
    /// The GraphQL definitions this document contains.
    /// </param>
    public DocumentNode(
        Location? location,
        IReadOnlyList<IDefinitionNode> definitions)
    {
        Location = location;
        Definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
    }

    /// <summary>
    /// Initializes a new instance of <see cref="DocumentNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the document in the parsed source text.
    /// </param>
    /// <param name="definitions">
    /// The GraphQL definitions this document contains.
    /// </param>
    /// <param name="nodesCount">
    /// The count of all nodes.
    /// </param>
    internal DocumentNode(
        Location? location,
        IReadOnlyList<IDefinitionNode> definitions,
        int nodesCount,
        int fieldsCount)
    {
        Location = location;
        Definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        _count = nodesCount;
        _fieldsCount = fieldsCount;
    }

    /// <inheritdoc />
    public SyntaxKind Kind => SyntaxKind.Document;

    /// <inheritdoc />
    public Location? Location { get; }

    /// <summary>
    /// Gets the documents definitions.
    /// </summary>
    public IReadOnlyList<IDefinitionNode> Definitions { get; }

    /// <summary>
    /// Gets the number of nodes in this document.
    /// </summary>
    public int Count
    {
        get
        {
            // the parser will always calculate the nodes efficiently and provide
            // us with the correct count.
            if (_count != -1)
            {
                return _count;
            }

            // in the case the document was constructed by hand or constructed through
            // rewriting a document we will calculate the nodes.
            var stack = new Stack<ISyntaxNode>(GetNodes());
            var count = 0;

            while (stack.Count > 0)
            {
                count++;

                foreach (var node in stack.Pop().GetNodes())
                {
                    stack.Push(node);
                }
            }

            // Since the calculation of the nodes requires us to walk the tree
            // we will cache the result on the document.
            _count = count;
            return _count;
        }
    }

    /// <summary>
    /// Gets the number of fields in this document.
    /// </summary>
    public int FieldsCount
    {
        get
        {
            // the parser will always calculate the nodes efficiently and provide
            // us with the correct count.
            if (_fieldsCount != -1)
            {
                return _fieldsCount;
            }

            // in the case the document was constructed by hand or constructed through
            // rewriting a document we will calculate the nodes.
            var stack = new Stack<ISyntaxNode>(GetNodes());
            var count = 0;

            while (stack.Count > 0)
            {
                var node = stack.Pop();

                if(node.Kind == SyntaxKind.Field)
                {
                    count++;
                }

                foreach (var child in node.GetNodes())
                {
                    stack.Push(child);
                }
            }

            // Since the calculation of the nodes requires us to walk the tree
            // we will cache the result on the document.
            _fieldsCount = count;
            return _fieldsCount;
        }
    }

    /// <inheritdoc />
    public IEnumerable<ISyntaxNode> GetNodes() => Definitions;

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
    public string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

    /// <summary>
    /// Creates a new instance that has all the characteristics of this
    /// documents but with a different location.
    /// </summary>
    /// <param name="location">
    /// The location that shall be applied to the new document.
    /// </param>
    /// <returns>
    /// Returns a new instance that has all the characteristics of this
    /// documents but with a different location.
    /// </returns>
    public DocumentNode WithLocation(Location? location)
        => new(location, Definitions);

    /// <summary>
    /// Creates a new instance that has all the characteristics of this
    /// documents but with different definitions.
    /// </summary>
    /// <param name="definitions">
    /// The definitions that shall be applied to the new document.
    /// </param>
    /// <returns>
    /// Returns a new instance that has all the characteristics of this
    /// documents but with a different definitions.
    /// </returns>
    public DocumentNode WithDefinitions(IReadOnlyList<IDefinitionNode> definitions)
        => new(Location, definitions);

    /// <summary>
    /// Gets an empty GraphQL document.
    /// </summary>
    public static DocumentNode Empty { get; } = new(null, Array.Empty<IDefinitionNode>());
}
