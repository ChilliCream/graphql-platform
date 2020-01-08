using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Utilities.Introspection
{
    public class IntrospectionClient : IIntrospectionClient
    {
        private static readonly JsonSerializerOptions _serializerOptions =
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

        public async Task DownloadSchemaAsync(
            HttpClient client,
            Stream stream,
            CancellationToken cancellationToken = default)
        {
            DocumentNode document = await DownloadSchemaAsync(
                client, cancellationToken)
                .ConfigureAwait(false);

            await Task.Run(
                () => SchemaSyntaxSerializer.Serialize(document, stream, true),
                cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<DocumentNode> DownloadSchemaAsync(
            HttpClient client,
            CancellationToken cancellationToken = default)
        {
            ISchemaFeatures features = await GetSchemaFeaturesAsync(
                client, cancellationToken)
                .ConfigureAwait(false);

            HttpQueryRequest request = IntrospectionQueryHelper.CreateIntrospectionQuery(features);

            IntrospectionResult result = await ExecuteIntrospectionAsync(
                client, request, cancellationToken)
                .ConfigureAwait(false);
            EnsureNoGraphQLErrors(result);

            return IntrospectionDeserializer.Deserialize(result);
        }

        public async Task<ISchemaFeatures> GetSchemaFeaturesAsync(
            HttpClient client,
            CancellationToken cancellationToken = default)
        {
            HttpQueryRequest request = IntrospectionQueryHelper.CreateFeatureQuery();

            IntrospectionResult result = await ExecuteIntrospectionAsync(
                client, request, cancellationToken)
                .ConfigureAwait(false);
            EnsureNoGraphQLErrors(result);

            return SchemaFeatures.FromIntrospectionResult(result);
        }

        private void EnsureNoGraphQLErrors(IntrospectionResult result)
        {
            if (result.Errors is { })
            {
                var message = new StringBuilder();

                for (int i = 0; i < result.Errors.Count; i++)
                {
                    if (i > 0)
                    {
                        message.AppendLine();
                    }
                    message.AppendLine(result.Errors[i].Message);
                }

                throw new IntrospectionException(message.ToString());
            }
        }

        private static async Task<IntrospectionResult> ExecuteIntrospectionAsync(
            HttpClient client,
            HttpQueryRequest request,
            CancellationToken cancellationToken)
        {
            byte[] serializedRequest = JsonSerializer.SerializeToUtf8Bytes(request);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, client.BaseAddress);
            httpRequest.Content = new ByteArrayContent(serializedRequest);
            HttpResponseMessage httpResponse =
                await client.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            httpResponse.EnsureSuccessStatusCode();

            using Stream stream = await httpResponse.Content.ReadAsStreamAsync()
                .ConfigureAwait(false);

            return await JsonSerializer.DeserializeAsync<IntrospectionResult>(
                stream, _serializerOptions, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
