namespace HotChocolate.Language;

/// <summary>
/// Represents the experimental parser options.
/// </summary>
public sealed class ParserOptionsExperimental
{
    internal ParserOptionsExperimental(
        bool allowFragmentVariables,
        bool allowFragmentArguments)
    {
        AllowFragmentVariables = allowFragmentVariables;
        AllowFragmentArguments = allowFragmentArguments;
    }

    /// <summary>
    /// <para>
    /// If enabled, the parser will parse the variable definitions of a fragment definition into
    /// <see cref="FragmentDefinitionNode.VariableDefinitions"/>.
    /// </para>
    /// <para>
    /// A variable definition uses the same syntax as an operation variable.
    /// </para>
    /// <code>
    /// fragment A($var: Boolean = false) on T
    /// {
    ///   ...
    /// }
    /// </code>
    /// <para>
    /// Note: this feature is experimental and may change or be removed in the future. Enabling
    /// <see cref="AllowFragmentArguments"/> also enables this.
    /// </para>
    /// </summary>
    public bool AllowFragmentVariables { get; }

    /// <summary>
    /// <para>
    /// If enabled, the parser will parse the variable definitions of a fragment definition into
    /// <see cref="FragmentDefinitionNode.VariableDefinitions"/>, and the arguments of a fragment
    /// spread into <see cref="FragmentSpreadNode.Arguments"/>.
    /// </para>
    /// <para>
    /// A variable definition uses the same syntax as an operation variable, and an argument uses
    /// the same syntax as a field argument.
    /// </para>
    /// <code>
    /// query
    /// {
    ///   ...A(var: true)
    /// }
    ///
    /// fragment A($var: Boolean = false) on T
    /// {
    ///   ...
    /// }
    /// </code>
    /// <para>
    /// Note: this feature is experimental and may change or be removed in the future.
    /// </para>
    /// </summary>
    public bool AllowFragmentArguments { get; }
}
