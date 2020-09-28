using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Utilities.Introspection
{
    public class IntrospectionClient : IIntrospectionClient
    {
        private const string _jsonContentType = "application/json";
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

        public static IntrospectionClient Default { get; } = new IntrospectionClient();

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
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            ISchemaFeatures features = await GetSchemaFeaturesAsync(
                client, cancellationToken)
                .ConfigureAwait(false);

            HttpQueryRequest request = IntrospectionQueryHelper.CreateIntrospectionQuery(features);

            IntrospectionResult result = await ExecuteIntrospectionAsync(
                client, request, cancellationToken)
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
        }

        private static async Task<IntrospectionResult> ExecuteIntrospectionAsync(
            HttpClient client,
            HttpQueryRequest request,
            CancellationToken cancellationToken)
        {
            byte[] serializedRequest = JsonSerializer.SerializeToUtf8Bytes(
                request, _serializerOptions);

            var content = new ByteArrayContent(serializedRequest);
            content.Headers.ContentType = new MediaTypeHeaderValue(_jsonContentType);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, client.BaseAddress)
            {
                Content = content
            };

            using HttpResponseMessage httpResponse =
                await client.SendAsync(httpRequest, cancellationToken)
                    .ConfigureAwait(false);
            httpResponse.EnsureSuccessStatusCode();

            using Stream stream =
                await httpResponse.Content.ReadAsStreamAsync()
                    .ConfigureAwait(false);

            return (await JsonSerializer.DeserializeAsync<IntrospectionResult>(
                stream, _serializerOptions, cancellationToken)
                .ConfigureAwait(false))!;
        }
    }
}
