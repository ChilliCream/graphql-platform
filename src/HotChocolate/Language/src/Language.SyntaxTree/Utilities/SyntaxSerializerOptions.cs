namespace HotChocolate.Language.Utilities;

public struct SyntaxSerializerOptions
{
    private int? _maxDirectivesPerLine;
    private int? _printWidth;

    /// <summary>
    /// Gets or sets a value that indicates whether the <see cref="SyntaxSerializer" />
    /// should format the GraphQL output, which includes indenting nested GraphQL tokens, adding
    /// new lines, and adding white space between property names and values.
    /// </summary>
    /// <value>
    /// <c>true</c> to format the GraphQL output; <c>false</c> to write without any extra
    /// white space. The default is false.
    /// </value>
    public bool Indented { get; set; }

    /// <summary>
    /// Gets or sets the maximum line width before the serializer breaks content
    /// onto multiple lines. Similar to Prettier's print width.
    /// </summary>
    /// <value>
    /// The maximum number of characters per line. The default is 80.
    /// </value>
    public int PrintWidth
    {
        get => _printWidth ?? 80;
        set => _printWidth = value;
    }

    /// <summary>
    /// Defines how many directives are allowed on the same line as
    /// the declaration before directives are put on separate lines.
    ///
    /// <code>
    /// type Foo @a @b @c {
    ///   bar: String
    /// }
    /// </code>
    ///
    /// <code>
    /// type Foo
    ///   @a
    ///   @b
    ///   @c {
    ///   bar: String
    /// }
    /// </code>
    /// </summary>
    public int MaxDirectivesPerLine
    {
        get => _maxDirectivesPerLine ?? int.MaxValue;
        set => _maxDirectivesPerLine = value;
    }
}
