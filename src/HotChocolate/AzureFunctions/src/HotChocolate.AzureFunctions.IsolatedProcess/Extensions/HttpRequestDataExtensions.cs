using HotChocolate.AzureFunctions.IsolatedProcess.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Net.Http.Headers;

namespace HotChocolate.AzureFunctions.IsolatedProcess;

public static class HttpRequestDataExtensions
{
    public static string GetContentType(
        this HttpRequestData httpRequestData,
        string defaultValue = GraphQLAzureFunctionsConstants.DefaultJsonContentType)
    {
        var contentType = httpRequestData.Headers.TryGetValues(
            HeaderNames.ContentType,
            out var contentTypeHeaders)
            ? contentTypeHeaders.FirstOrDefault()
            : defaultValue;

        return contentType ?? defaultValue;
    }

    public static HttpRequestData SetCurrentHttpContext(this HttpRequestData httpRequestData, HttpContext httpContext)
    {
        httpRequestData.FunctionContext.InstanceServices.SetCurrentHttpContext(httpContext);
        return httpRequestData;
    }

    public static async Task<string?> ReadResponseContentAsync(
        this HttpResponseData httpResponseData)
        => await httpResponseData.Body.ReadStreamAsStringAsync().ConfigureAwait(false);
}
