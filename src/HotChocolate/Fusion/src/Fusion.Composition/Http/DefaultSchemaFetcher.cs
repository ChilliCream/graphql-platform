using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion;

internal static class DefaultSchemaFetcher
{
    public static async Task<string> FetchAsync(
        HttpClient client,
        string sourceSchemaName,
        Uri endpoint,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrEmpty(sourceSchemaName);
        ArgumentNullException.ThrowIfNull(endpoint);

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                string.Format(
                    DefaultSchemaFetcher_RequestFailed,
                    sourceSchemaName,
                    (int)response.StatusCode,
                    response.ReasonPhrase));
        }

        var sourceText = await SchemaHttpResponseReader.ReadAsStringAsync(
            response.Content,
            sourceSchemaName,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(sourceText))
        {
            throw new InvalidOperationException(
                string.Format(
                    DefaultSchemaFetcher_EmptyResponse,
                    sourceSchemaName));
        }

        return sourceText;
    }
}
