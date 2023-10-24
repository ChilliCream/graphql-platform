using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Utilities.Introspection;

/// <summary>
/// A utility for introspecting a GraphQL server and
/// downloading the introspection result as GraphQL SDL.
/// </summary>
public static class IntrospectionClient
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
    
    internal static JsonSerializerOptions SerializerOptions => _serializerOptions;

    /// <summary>
    /// Downloads the schema information from a GraphQL server
    /// and writes it as GraphQL SDL to the given stream.
    /// </summary>
    /// <param name="client">
    /// The HttpClient that shall be used to execute the introspection query.
    /// </param>
    /// <param name="stream">
    /// The stream to which the schema shall be written to.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    public static async Task DownloadSchemaAsync(
        HttpClient client,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        if (client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }
        
        using var internalClient = GraphQLHttpClient.Create(client, disposeHttpClient: false);
        await DownloadSchemaAsync(internalClient, stream, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Downloads the schema information from a GraphQL server
    /// and writes it as GraphQL SDL to the given stream.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> that shall be used to execute the introspection query.
    /// </param>
    /// <param name="stream">
    /// The stream to which the schema shall be written to.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    public static async Task DownloadSchemaAsync(
        GraphQLHttpClient client,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        if (client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        var document =
            await DownloadSchemaAsync(client, cancellationToken)
                .ConfigureAwait(false);

        await document
            .PrintToAsync(stream, true, cancellationToken)
            .ConfigureAwait(false);
    }
    
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
    public static async Task<DocumentNode> DownloadSchemaAsync(
        HttpClient client, 
        CancellationToken cancellationToken = default)
    {
        if (client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }
        
        using var internalClient = GraphQLHttpClient.Create(client, disposeHttpClient: false);
        return await DownloadSchemaAsync(internalClient, cancellationToken).ConfigureAwait(false);
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
    public static async Task<DocumentNode> DownloadSchemaAsync(
        GraphQLHttpClient client,
        CancellationToken cancellationToken = default)
    {
        if (client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        var features = await GetSchemaFeaturesAsync(client, cancellationToken).ConfigureAwait(false);

        var request = IntrospectionQueryHelper.CreateIntrospectionQuery(features);

        var result = 
            await ExecuteIntrospectionAsync(client, request, cancellationToken)
                .ConfigureAwait(false);
        
        EnsureNoGraphQLErrors(result);

        return IntrospectionDeserializer.Deserialize(result).RemoveBuiltInTypes();
    }
    
    /// <summary>
    /// Gets the supported GraphQL features from the server by doing an introspection query.
    /// </summary>
    /// <param name="client">
    /// The HttpClient that shall be used to execute the introspection query.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>Returns an object that indicates what features are supported.</returns>
    public static async Task<ISchemaFeatures> GetSchemaFeaturesAsync(
        HttpClient client, 
        CancellationToken cancellationToken = default)
    {
        if (client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }
        
        using var internalClient = new DefaultGraphQLHttpClient(client, disposeInnerClient: false);
        return await GetSchemaFeaturesAsync(internalClient, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the supported GraphQL features from the server by doing an introspection query.
    /// </summary>
    /// <param name="client">
    /// The <see cref="GraphQLHttpClient"/> that shall be used to execute the introspection query.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>Returns an object that indicates what features are supported.</returns>
    public static async Task<ISchemaFeatures> GetSchemaFeaturesAsync(
        GraphQLHttpClient client,
        CancellationToken cancellationToken = default)
    {
        if (client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        var request = IntrospectionQueryHelper.CreateFeatureQuery();

        var result =
            await ExecuteIntrospectionAsync(client, request, cancellationToken)
                .ConfigureAwait(false);

        EnsureNoGraphQLErrors(result);

        return SchemaFeatures.FromIntrospectionResult(result);
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

    private static async Task<IntrospectionResult> ExecuteIntrospectionAsync(
        GraphQLHttpClient client,
        OperationRequest operation,
        CancellationToken cancellationToken)
    {
        var request = new GraphQLHttpRequest(operation)
        {
            Method = GraphQLHttpMethod.Post,
            EnableFileUploads = false,
        };

        var response =
            await client.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        IntrospectionData? data = null;
        IReadOnlyList<IntrospectionError>? errors = null;

        var result = await response.ReadAsResultAsync(cancellationToken).ConfigureAwait(false);

        if (result.Data.ValueKind is JsonValueKind.Object)
        {
            data = result.Data.Deserialize<IntrospectionData>(_serializerOptions);
        }
        
        if (result.Errors.ValueKind is JsonValueKind.Array)
        {
            errors = result.Errors.Deserialize<IntrospectionError[]>(_serializerOptions);
        }

        return new IntrospectionResult(data, errors);
    }
}