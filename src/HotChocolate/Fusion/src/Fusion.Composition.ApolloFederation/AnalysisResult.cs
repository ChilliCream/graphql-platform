using HotChocolate.Fusion.Errors;
using HotChocolate.Language;

namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Contains the metadata extracted from analyzing a Federation v2 schema document.
/// </summary>
internal sealed class AnalysisResult
{
    /// <summary>
    /// Gets the detected federation version string (e.g. "v2.0", "v2.5").
    /// A value of "v1" indicates unsupported federation v1.
    /// </summary>
    public string FederationVersion { get; set; } = "v1";

    /// <summary>
    /// Gets the entity key definitions keyed by type name.
    /// Each type may have multiple <c>@key</c> directives.
    /// </summary>
    public Dictionary<string, List<EntityKeyInfo>> EntityKeys { get; init; } = [];

    /// <summary>
    /// Gets the field type map: typeName -> fieldName -> field type node.
    /// </summary>
    public Dictionary<string, Dictionary<string, ITypeNode>> TypeFieldTypes { get; init; } = [];

    /// <summary>
    /// Gets the name of the query root type (defaults to "Query").
    /// </summary>
    public string QueryTypeName { get; set; } = "Query";

    /// <summary>
    /// Gets the list of composition errors detected during analysis.
    /// </summary>
    public List<CompositionError> Errors { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether analysis produced any errors.
    /// </summary>
    public bool HasErrors => Errors.Count > 0;
}
