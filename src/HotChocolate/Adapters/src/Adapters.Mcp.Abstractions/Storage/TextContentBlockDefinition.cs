namespace HotChocolate.Adapters.Mcp.Storage;

public sealed class TextContentBlockDefinition(string text) : IContentBlockDefinition
{
    /// <inheritdoc/>
    public string Type { get; } = "text";

    /// <summary>
    /// Gets the text content.
    /// </summary>
    public string Text { get; init; } = text;
}
