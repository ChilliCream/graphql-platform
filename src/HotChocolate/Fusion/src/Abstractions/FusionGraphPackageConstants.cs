namespace HotChocolate.Fusion;

/// <summary>
/// Defines constants that are used in the fusion graph package.
/// </summary>
public static class FusionGraphPackageConstants
{
    /// <summary>
    /// Gets the relationship kind of the fusion graph document.
    /// </summary>
    public const string FusionKind = "urn:hotchocolate:fusion:graph";

    /// <summary>
    /// Gets the file name of the fusion graph document.
    /// </summary>
    public const string FusionFileName = "fusion.graphql";

    /// <summary>
    /// Gets the relationship kind of the fusion graph document.
    /// </summary>
    public const string FusionSettingsKind = "urn:hotchocolate:fusion:settings";

    /// <summary>
    /// Gets the file name of the fusion graph document.
    /// </summary>
    public const string FusionSettingsFileName = "fusion-settings.json";

    /// <summary>
    /// Gets relationship id of the fusion graph document.
    /// </summary>
    public const string FusionId = "fusion";

    /// <summary>
    /// Gets relationship id of the fusion graph settings document.
    /// </summary>
    public const string FusionSettingsId = "fusion-settings";

    /// <summary>
    /// Gets the relationship kind of a GraphQL schema document.
    /// </summary>
    public const string SchemaKind = "urn:graphql:schema";

    /// <summary>
    /// Gets the file name of a GraphQL schema document.
    /// </summary>
    public const string SchemaFileName  = "schema.graphql";

    /// <summary>
    /// Gets the relationship id of the root GraphQL schema document in a package.
    /// </summary>
    public const string SchemaId = "schema";

    /// <summary>
    /// Gets the media type of a GraphQL schema document.
    /// </summary>
    public const string SchemaMediaType = "application/graphql-schema";

    /// <summary>
    /// Gets the media type of a GraphQL schema document.
    /// </summary>
    public const string JsonMediaType = "application/json";

    /// <summary>
    /// Gets the file name of the subgraph config document.
    /// </summary>
    public const string SubgraphConfigFileName = "subgraph-config.json";

    /// <summary>
    /// Gets the relationship kind of the subgraph config document.
    /// </summary>
    public const string SubgraphConfigKind = "urn:hotchocolate:fusion:subgraph-config";

    /// <summary>
    /// Gets the relationship kind of a GraphQL schema extension document.
    /// </summary>
    public const string ExtensionKind = "urn:graphql:schema-extensions";
}
