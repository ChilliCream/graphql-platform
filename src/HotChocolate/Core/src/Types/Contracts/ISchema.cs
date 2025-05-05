using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types.Relay;

#nullable enable

namespace HotChocolate;

/// <summary>
/// A GraphQL Schema defines the capabilities of a GraphQL server. It
/// exposes all available types and directives on the server, as well as
/// the entry points for query, mutation, and subscription operations.
/// </summary>
public interface ISchema
    : ISchemaDefinition
    , IHasReadOnlyContextData
    , INodeIdRuntimeTypeLookup
    , IFeatureProvider
{
    /// <summary>
    /// Gets the global schema services.
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// Tries to get the .net type representation of a schema type.
    /// </summary>
    /// <param name="typeName">The name of the type.</param>
    /// <param name="runtimeType">The resolved .net type.</param>
    /// <returns>
    /// <c>true</c>, if a .net type was found that was bound
    /// the specified schema type, <c>false</c> otherwise.
    /// </returns>
    bool TryGetRuntimeType(string typeName, [NotNullWhen(true)] out Type? runtimeType);

    /// <summary>
    /// Generates a schema document.
    /// </summary>
    DocumentNode ToDocument(bool includeSpecScalars = false);

    /// <summary>
    /// Prints the schema SDL representation
    /// </summary>
    string ToString();
}
