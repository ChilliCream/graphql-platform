namespace HotChocolate.ModelContextProtocol.Directives;

internal sealed class McpToolDirective
{
    public string? Title { get; init; }

    /// <summary>
    /// If <c>true</c>, the tool may perform destructive updates to its environment. If
    /// <c>false</c>, the tool performs only additive updates.
    /// </summary>
    public bool? DestructiveHint { get; init; }

    /// <summary>
    /// If <c>true</c>, calling the tool repeatedly with the same arguments will have no additional
    /// effect on its environment.
    /// </summary>
    public bool? IdempotentHint { get; init; }

    /// <summary>
    /// If <c>true</c>, this tool may interact with an “open world” of external entities. If
    /// <c>false</c>, the tool’s domain of interaction is closed. For example, the world of a web
    /// search tool is open, whereas that of a memory tool is not.
    /// </summary>
    public bool? OpenWorldHint { get; init; }
}
