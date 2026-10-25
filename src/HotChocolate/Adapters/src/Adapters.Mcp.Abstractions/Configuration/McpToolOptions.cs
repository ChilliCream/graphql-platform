namespace HotChocolate.Adapters.Mcp.Configuration;

/// <summary>
/// Holds the per-schema options that control how MCP tools are generated from operations.
/// </summary>
public sealed class McpToolOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether named input object types are emitted in a tool's
    /// input schema as JSON Schema definitions (<c>$defs</c>) referenced through <c>$ref</c>.
    /// <para>
    /// When <c>true</c> (the default), each named input object type is emitted once under
    /// <c>$defs</c> and referenced by <c>$ref</c>. This is the only way to represent recursive
    /// input types (such as filter inputs that reference themselves through <c>and</c>/<c>or</c>)
    /// in a finite schema.
    /// </para>
    /// <para>
    /// When <c>false</c>, input object types are inlined. This suits MCP clients that do not
    /// support JSON Schema references. Where a type refers to itself, that point is collapsed to
    /// a generic object (<c>{ "type": "object" }</c>) and the rest of the schema keeps its full
    /// structure.
    /// </para>
    /// </summary>
    public bool UseJsonSchemaReferences { get; set; } = true;
}
