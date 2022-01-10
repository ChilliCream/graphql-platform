namespace HotChocolate.Language.Visitors;

/// <summary>
/// Represents basic visitor options.
/// </summary>
public struct SyntaxVisitorOptions
{
    /// <summary>
    /// Specifies if the visitor shall traverse name nodes.
    /// </summary>
    public bool VisitNames { get; set; }

    /// <summary>
    /// Specifies if the visitor shall traverse description nodes.
    /// </summary>
    public bool VisitDescriptions { get; set; }

    /// <summary>
    /// Specifies if the visitor shall traverse directives nodes.
    /// </summary>
    public bool VisitDirectives { get; set; }

    /// <summary>
    /// Specifies if the visitor shall traverse argument nodes.
    /// </summary>
    public bool VisitArguments { get; set; }
}
