namespace HotChocolate.Fusion.Language;

internal struct SyntaxSerializerOptions
{
    /// <summary>
    /// Gets or sets a value that indicates whether the syntax serializer should format the GraphQL
    /// output, which includes indenting nested GraphQL tokens and adding new lines.
    /// </summary>
    /// <value>
    /// <c>true</c> to format the GraphQL output; <c>false</c> to write without any extra
    /// white space. The default is <c>false</c>.
    /// </value>
    public bool Indented { get; set; }
}
