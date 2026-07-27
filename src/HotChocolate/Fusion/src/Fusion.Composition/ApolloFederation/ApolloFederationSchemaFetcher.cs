using System.Text;
using System.Text.Json;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion;

internal static class ApolloFederationSchemaFetcher
{
    private const string RequestBody =
        """{"query":"query FusionServiceSdl { _service { sdl } }"}""";

    public static async Task<string> FetchAsync(
        HttpClient client,
        string sourceSchemaName,
        Uri endpoint,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrEmpty(sourceSchemaName);
        ArgumentNullException.ThrowIfNull(endpoint);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(RequestBody, Encoding.UTF8, "application/json")
        };

        var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    string.Format(
                        ApolloFederationSchemaFetcher_RequestFailed,
                        sourceSchemaName,
                        (int)response.StatusCode,
                        response.ReasonPhrase));
            }

            var responseBody = await SchemaHttpResponseReader.ReadAsStringAsync(
                response.Content,
                sourceSchemaName,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                throw new InvalidOperationException(
                    string.Format(
                        ApolloFederationSchemaFetcher_EmptyResponse,
                        sourceSchemaName));
            }

            JsonDocument document;

            try
            {
                document = JsonDocument.Parse(responseBody);
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException(
                    string.Format(
                        ApolloFederationSchemaFetcher_InvalidResponse,
                        sourceSchemaName),
                    exception);
            }

            using (document)
            {
                var root = document.RootElement;

                if (root.ValueKind is not JsonValueKind.Object)
                {
                    throw InvalidResponse(sourceSchemaName);
                }

                if (root.TryGetProperty("errors", out var errors))
                {
                    if (errors.ValueKind is not JsonValueKind.Array)
                    {
                        throw InvalidResponse(sourceSchemaName);
                    }

                    var messages = new List<string>();

                    foreach (var error in errors.EnumerateArray())
                    {
                        if (error.ValueKind is not JsonValueKind.Object
                            || !error.TryGetProperty("message", out var message)
                            || message.ValueKind is not JsonValueKind.String
                            || string.IsNullOrWhiteSpace(message.GetString()))
                        {
                            throw InvalidResponse(sourceSchemaName);
                        }

                        messages.Add(message.GetString()!);
                    }

                    if (messages.Count > 0)
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                ApolloFederationSchemaFetcher_QueryRejected,
                                sourceSchemaName,
                                string.Join("; ", messages)));
                    }
                }

                if (!root.TryGetProperty("data", out var data)
                    || data.ValueKind is not JsonValueKind.Object
                    || !data.TryGetProperty("_service", out var service)
                    || service.ValueKind is not JsonValueKind.Object
                    || !service.TryGetProperty("sdl", out var sdl)
                    || sdl.ValueKind is not JsonValueKind.String
                    || sdl.GetString() is not { } sourceText
                    || string.IsNullOrWhiteSpace(sourceText))
                {
                    throw new InvalidOperationException(
                        string.Format(
                            ApolloFederationSchemaFetcher_MissingServiceSdl,
                            sourceSchemaName));
                }

                return sourceText;
            }
        }
    }

    private static InvalidOperationException InvalidResponse(string sourceSchemaName)
        => new(string.Format(
            ApolloFederationSchemaFetcher_InvalidResponse,
            sourceSchemaName));
}
