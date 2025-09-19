namespace HotChocolate.Language.Utilities;

public struct SyntaxSerializerOptions
{
    private int? _maxDirectivesPerLine;

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
