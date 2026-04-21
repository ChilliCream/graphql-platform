using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.Fusion.Execution.Clients;

namespace HotChocolate.Fusion.Configuration;

/// <summary>
/// Parses a source-schema settings element into an <see cref="ISourceSchemaClientConfiguration"/>.
/// </summary>
public interface ISourceSchemaClientConfigurationParser
{
    /// <summary>
    /// Attempts to build a configuration for the specified source schema and transport.
    /// </summary>
    /// <param name="sourceSchema">
    /// The source-schema JSON property. <c>sourceSchema.Name</c> is the schema name;
    /// <c>sourceSchema.Value</c> is the per-schema settings object (including <c>transports</c>
    /// and optional <c>extensions</c>).
    /// </param>
    /// <param name="transport">
    /// The specific transport JSON property currently being offered. <c>transport.Name</c> is the
    /// transport kind (<c>"http"</c>, <c>"websockets"</c>, ...) and <c>transport.Value</c> is the
    /// transport element.
    /// </param>
    /// <param name="configuration">
    /// When this method returns <see langword="true"/>, contains the parsed
    /// <see cref="ISourceSchemaClientConfiguration"/>; otherwise, <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the parser claimed the transport and produced a configuration;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    bool TryParse(
        JsonProperty sourceSchema,
        JsonProperty transport,
        [NotNullWhen(true)] out ISourceSchemaClientConfiguration? configuration);
}
