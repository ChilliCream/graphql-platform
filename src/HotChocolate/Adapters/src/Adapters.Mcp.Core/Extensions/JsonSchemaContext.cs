using Json.Schema;

namespace HotChocolate.Adapters.Mcp.Extensions;

/// <summary>
/// Carries the state shared across a single JSON schema generation walk: the accumulated
/// definitions, the set of input object types currently being expanded (for cycle detection),
/// and whether references are used.
/// </summary>
internal sealed class JsonSchemaContext
{
    /// <summary>
    /// Gets the named input object type definitions discovered during the walk, keyed by type
    /// name. These are emitted at the root of the schema under <c>$defs</c>.
    /// </summary>
    public Dictionary<string, JsonSchema> Defs { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the names of the input object types currently being expanded on the active path.
    /// Used to detect cycles so the walk terminates on self-referencing types.
    /// </summary>
    public HashSet<string> Building { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets a value indicating whether named input object types are emitted as <c>$defs</c>
    /// referenced through <c>$ref</c> (<c>true</c>) or inlined (<c>false</c>).
    /// </summary>
    public bool UseReferences { get; init; } = true;
}
