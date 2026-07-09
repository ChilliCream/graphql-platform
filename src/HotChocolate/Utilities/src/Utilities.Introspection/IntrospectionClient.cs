using System.Text;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using static HotChocolate.Utilities.Introspection.CapabilityInspector;
using static HotChocolate.Utilities.Introspection.IntrospectionQueryHelper;

namespace HotChocolate.Utilities.Introspection;

/// <summary>
/// A utility for inspecting GraphQL server feature support and for introspecting a GraphQL server.
/// </summary>
public static class IntrospectionClient
{
    private static readonly JsonSerializerOptions s_serializerOptions =
        new(IntrospectionJsonSerializerContext.Default.Options);

    internal static JsonSerializerOptions SerializerOptions => s_serializerOptions;

    /// <summary>
    /// Downloads the schema information from a GraphQL server
    /// and returns the schema syntax tree.
    /// </summary>
    /// <param name="client">
    /// The HttpClient that shall be used to execute the introspection query.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>Returns a parsed GraphQL schema syntax tree.</returns>
    public static Task<DocumentNode> IntrospectServerAsync(
        HttpClient client,
        CancellationToken cancellationToken = default)
        => IntrospectServerAsync(client, default, cancellationToken);

    /// <summary>
    /// Downloads the schema information from a GraphQL server
    /// and returns the schema syntax tree.
    /// </summary>
    /// <param name="client">
    /// The HttpClient that shall be used to execute the introspection query.
    /// </param>
    /// <param name="options">
    /// The introspection options.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>Returns a parsed GraphQL schema syntax tree.</returns>
    public static Task<DocumentNode> IntrospectServerAsync(
        HttpClient client,
        IntrospectionOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        return IntrospectServerInternalAsync(client, options, cancellationToken);
    }

    private static async Task<DocumentNode> IntrospectServerInternalAsync(
        HttpClient client,
        IntrospectionOptions options,
        CancellationToken cancellationToken = default)
    {
        using var internalClient = GraphQLHttpClient.Create(client, disposeHttpClient: false);
        return await IntrospectServerInternalAsync(internalClient, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Downloads the schema information from a GraphQL server
    /// and returns the schema syntax tree.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> that shall be used to execute the introspection query.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>Returns a parsed GraphQL schema syntax tree.</returns>
    public static Task<DocumentNode> IntrospectServerAsync(
        GraphQLHttpClient client,
        CancellationToken cancellationToken = default)
        => IntrospectServerAsync(client, default, cancellationToken);

    /// <summary>
    /// Downloads the schema information from a GraphQL server
    /// and returns the schema syntax tree.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> that shall be used to execute the introspection query.
    /// </param>
    /// <param name="options">
    /// The introspection options.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>Returns a parsed GraphQL schema syntax tree.</returns>
    public static Task<DocumentNode> IntrospectServerAsync(
        GraphQLHttpClient client,
        IntrospectionOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        return IntrospectServerInternalAsync(client, options, cancellationToken);
    }

    private static async Task<DocumentNode> IntrospectServerInternalAsync(
        GraphQLHttpClient client,
        IntrospectionOptions options,
        CancellationToken cancellationToken = default)
    {
        var capabilities = await InspectServerAsync(client, options, cancellationToken).ConfigureAwait(false);
        var result = await IntrospectAsync(client, capabilities, options, cancellationToken).ConfigureAwait(false);

        EnsureNoGraphQLErrors(result);

        return IntrospectionFormatter.Format(result).RemoveBuiltInTypes();
    }

    /// <summary>
    /// Gets the supported GraphQL server capabilities from the server by doing an introspection query.
    /// </summary>
    /// <param name="client">
    /// The HttpClient that shall be used to execute the introspection query.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>Returns an object that indicates what capabilities the GraphQL server has.</returns>
    public static Task<ServerCapabilities> InspectServerAsync(
        HttpClient client,
        CancellationToken cancellationToken = default)
        => InspectServerAsync(client, default, cancellationToken);

    /// <summary>
    /// Gets the supported GraphQL server capabilities from the server by doing an introspection query.
    /// </summary>
    /// <param name="client">
    /// The HttpClient that shall be used to execute the introspection query.
    /// </param>
    /// <param name="options">
    /// The introspection options.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>Returns an object that indicates what capabilities the GraphQL server has.</returns>
    public static Task<ServerCapabilities> InspectServerAsync(
        HttpClient client,
        IntrospectionOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        return InspectServerInternalAsync(client, options, cancellationToken);
    }

    private static async Task<ServerCapabilities> InspectServerInternalAsync(
        HttpClient client,
        IntrospectionOptions options,
        CancellationToken cancellationToken = default)
    {
        using var internalClient = GraphQLHttpClient.Create(client, disposeHttpClient: false);
        return await InspectAsync(internalClient, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the supported GraphQL server capabilities from the server by doing an introspection query.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> that shall be used to execute the introspection query.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>Returns an object that indicates what capabilities the GraphQL server has.</returns>
    public static Task<ServerCapabilities> InspectServerAsync(
        GraphQLHttpClient client,
        CancellationToken cancellationToken = default)
        => InspectServerAsync(client, default, cancellationToken);

    /// <summary>
    /// Gets the supported GraphQL server capabilities from the server by doing an introspection query.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> that shall be used to execute the introspection query.
    /// </param>
    /// <param name="options">
    /// The introspection options.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>Returns an object that indicates what capabilities the GraphQL server has.</returns>
    public static Task<ServerCapabilities> InspectServerAsync(
        GraphQLHttpClient client,
        IntrospectionOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        return InspectAsync(client, options, cancellationToken);
    }

    private static void EnsureNoGraphQLErrors(IntrospectionResult result)
    {
        if (result.Errors is null)
        {
            return;
        }

        var message = new StringBuilder();

        for (var i = 0; i < result.Errors.Count; i++)
        {
            if (i > 0)
            {
                message.AppendLine();
            }
            message.AppendLine(result.Errors[i].Message);
        }

        throw new IntrospectionException(message.ToString());
    }

    private static async Task<IntrospectionResult> IntrospectAsync(
        GraphQLHttpClient client,
        ServerCapabilities features,
        IntrospectionOptions options,
        CancellationToken cancellationToken)
    {
        var request = CreateIntrospectionRequest(features, options);

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        using var result = await response.ReadAsResultAsync(cancellationToken).ConfigureAwait(false);

        IntrospectionData? data = null;
        IReadOnlyList<IntrospectionError>? errors = null;

        if (result.Data.ValueKind is JsonValueKind.Object)
        {
            data = result.Data.Deserialize(IntrospectionJsonSerializerContext.Default.IntrospectionData);
        }

        if (result.Errors.ValueKind is JsonValueKind.Array)
        {
            errors = result.Errors.Deserialize(IntrospectionJsonSerializerContext.Default.IntrospectionErrorArray);
        }

        return new IntrospectionResult(data, errors);
    }
}
