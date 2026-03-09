namespace HotChocolate.Fusion.Language;

public sealed class FieldSelectionMapParserOptions(
    bool noLocations = false,
    int maxAllowedNodes = int.MaxValue,
    int maxAllowedTokens = int.MaxValue)
{
    /// <summary>
    /// By default, the parser creates <see cref="IFieldSelectionMapSyntaxNode" />s that know the
    /// location in the source that they correspond to. This configuration flag disables that
    /// behavior for performance or testing.
    /// </summary>
    public bool NoLocations { get; } = noLocations;

    /// <summary>
    /// <para>
    /// Parser CPU and memory usage is linear to the number of nodes in the source text, however in
    /// extreme cases it becomes quadratic due to memory exhaustion. Parsing happens before
    /// validation, so even invalid queries can burn lots of CPU time and memory.
    /// </para>
    /// <para>
    /// To prevent this you can set a maximum number of nodes allowed within the source text.
    /// </para>
    /// </summary>
    public int MaxAllowedNodes { get; } = maxAllowedNodes;

    /// <summary>
    /// <para>
    /// Parser CPU and memory usage is linear to the number of tokens in the source text, however in
    /// extreme cases it becomes quadratic due to memory exhaustion. Parsing happens before
    /// validation so even invalid queries can burn lots of CPU time and memory.
    /// </para>
    /// <para>
    /// To prevent this you can set a maximum number of tokens allowed within the source text.
    /// </para>
    /// </summary>
    public int MaxAllowedTokens { get; } = maxAllowedTokens;

    /// <summary>
    /// Gets the default parser options.
    /// </summary>
    public static FieldSelectionMapParserOptions Default { get; } = new();
}
