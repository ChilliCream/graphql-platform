using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Net.Http.Headers;

namespace HotChocolate.AzureFunctions.IsolatedProcess;

public static class HttpRequestDataExtensions
{
    public static string GetContentType(this HttpRequestData httpRequestData, string defaultValue = GraphQLAzureFunctionsConstants.DefaultJsonContentType)
    {
        var contentType = httpRequestData.Headers.TryGetValues(HeaderNames.ContentType, out IEnumerable<string>? contentTypeHeaders)
            ? contentTypeHeaders.FirstOrDefault()
            : defaultValue;

        return contentType ?? GraphQLAzureFunctionsConstants.DefaultJsonContentType;
    }
}
