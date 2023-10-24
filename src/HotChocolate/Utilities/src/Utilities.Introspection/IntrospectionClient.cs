using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
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

[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class IntrospectionClient : IIntrospectionClient
{
    private static readonly JsonSerializerOptions _serializerOptions;

#pragma warning disable CA1810
    static IntrospectionClient()
    {
        var options = new JsonSerializerOptions();
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.Converters.Add(new JsonStringEnumConverter());
        _serializerOptions = options;
    }
#pragma warning restore CA1810

    internal static JsonSerializerOptions SerializerOptions => _serializerOptions;

    public static IntrospectionClient Default { get; } = new();

    public async Task DownloadSchemaAsync(
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
        
        using var internalClient = new DefaultGraphQLHttpClient(client, disposeInnerClient: false);
        await DownloadSchemaAsync(internalClient, stream, cancellationToken).ConfigureAwait(false);
    }

    public async Task DownloadSchemaAsync(
        IGraphQLHttpClient client,
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
    
    public Task<DocumentNode> DownloadSchemaAsync(
        HttpClient client, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<DocumentNode> DownloadSchemaAsync(
        IGraphQLHttpClient client,
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
    
    public async Task<ISchemaFeatures> GetSchemaFeaturesAsync(
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

    public async Task<ISchemaFeatures> GetSchemaFeaturesAsync(
        IGraphQLHttpClient client,
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

    private void EnsureNoGraphQLErrors(IntrospectionResult result)
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
        IGraphQLHttpClient client,
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