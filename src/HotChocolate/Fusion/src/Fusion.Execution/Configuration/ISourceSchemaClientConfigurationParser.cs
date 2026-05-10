using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Configuration;

/// <summary>
/// Parses a source-schema settings element into one or more
/// <see cref="ISourceSchemaClientConfiguration"/> instances.
/// </summary>
public interface ISourceSchemaClientConfigurationParser
{
    /// <summary>
    /// Attempts to claim a source schema and produce its client configurations.
    /// </summary>
    /// <param name="schema">
    /// The completed <see cref="FusionSchemaDefinition"/>. Parsers consult
    /// <see cref="FusionSchemaDefinition.GetSourceSchemaConnectorKind"/> to decide whether
    /// to claim the source schema.
    /// </param>
    /// <param name="sourceSchema">
    /// The source-schema JSON property. <c>sourceSchema.Name</c> is the schema name;
    /// <c>sourceSchema.Value</c> is the per-schema settings object (including <c>transports</c>).
    /// </param>
    /// <param name="configurations">
    /// When this method returns <see langword="true"/>, contains the parsed
    /// <see cref="ISourceSchemaClientConfiguration"/> instances; otherwise, <see langword="null"/>.
    /// A parser may return multiple configurations for one source schema (for example one for
    /// HTTP and one for WebSocket).
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the parser claimed the source schema and produced configurations;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    bool TryParse(
        FusionSchemaDefinition schema,
        JsonProperty sourceSchema,
        [NotNullWhen(true)] out ISourceSchemaClientConfiguration[]? configurations);
}
