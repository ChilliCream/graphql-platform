namespace HotChocolate.Language;

/// <summary>
/// Represents the experimental parser options.
/// </summary>
public sealed class ParserOptionsExperimental
{
    internal ParserOptionsExperimental(bool allowFragmentVariables)
    {
        AllowFragmentVariables = allowFragmentVariables;
    }

    /// <summary>
    /// If enabled, the parser will understand and parse variable
    /// definitions contained in a fragment definition.They'll be
    /// represented in the `variableDefinitions` field of the
    /// FragmentDefinitionNode.
    ///
    /// The syntax is identical to normal, query-defined variables.
    /// For example:
    ///
    /// fragment A($var: Boolean = false) on T
    /// {
    ///    ...
    /// }
    ///
    /// Note: this feature is experimental and may change or be
    /// removed in the future.
    /// </summary>
    public bool AllowFragmentVariables { get; }
}
