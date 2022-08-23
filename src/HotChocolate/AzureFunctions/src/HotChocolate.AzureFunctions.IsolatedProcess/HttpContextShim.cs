using System.Net;
using HotChocolate.AzureFunctions.IsolatedProcess.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Primitives;

namespace HotChocolate.AzureFunctions.IsolatedProcess;

public class HttpContextShim : IDisposable
{
    private bool _disposed;

    public HttpContextShim(HttpContext httpContext)
    {
        HttpContext = httpContext ??
            throw new ArgumentNullException(nameof(httpContext));
        IsolatedProcessHttpRequestData = null;
    }

    public HttpContextShim(HttpContext httpContext, HttpRequestData? httpRequestData)
    {
        HttpContext = httpContext ??
            throw new ArgumentNullException(nameof(httpContext));
        IsolatedProcessHttpRequestData = httpRequestData ??
            throw new ArgumentNullException(nameof(httpRequestData));
    }

    protected HttpRequestData? IsolatedProcessHttpRequestData { get; set; }

    //Must keep the Reference so we can safely Dispose!
    public HttpContext HttpContext { get; protected set; }

    /// <summary>
    /// Factory method to Create an HttpContext that is AspNetCore compatible.
    /// All pertinent data from the HttpRequestData provided by the
    /// Azure Functions Isolated Process will be marshalled
    /// into the HttpContext for HotChocolate to consume.
    /// NOTE: This is done as Factory method (and not in the Constructor)
    /// to support optimized Async reading of incoming Request Content/Stream.
    /// </summary>
    public static Task<HttpContextShim> CreateHttpContextAsync(HttpRequestData httpRequestData)
    {
        if (httpRequestData == null)
        {
            throw new ArgumentNullException(nameof(httpRequestData));
        }

        // Factored out Async logic to Address SonarCloud concern for exceptions in Async flow...
        return CreateHttpContextInternalAsync(httpRequestData);
    }

    private static async Task<HttpContextShim> CreateHttpContextInternalAsync(
        HttpRequestData httpRequestData)
    {
        var requestBody = await httpRequestData.ReadAsStringAsync().ConfigureAwait(false);

        var httpContext = new HttpContextBuilder().CreateHttpContext(
            requestHttpMethod: httpRequestData.Method,
            requestUri: httpRequestData.Url,
            requestBody: requestBody,
            requestBodyContentType: httpRequestData.GetContentType(),
            requestHeaders: httpRequestData.Headers,
            claimsIdentities: httpRequestData.Identities
        );

        // Ensure we track the HttpContext internally for cleanup when disposed!
        return new HttpContextShim(httpContext, httpRequestData);
    }

    /// <summary>
    /// Create an HttpResponseData containing the proxied response content results;
    /// marshalled back from the HttpContext.
    /// </summary>
    /// <returns></returns>
    public async Task<HttpResponseData> CreateHttpResponseDataAsync()
    {
        var httpContext = HttpContext
            ?? throw new NullReferenceException(
                "The HttpContext has not been initialized correctly.");

        var httpRequestData = IsolatedProcessHttpRequestData
            ?? throw new NullReferenceException(
                "The HttpRequestData has not been initialized correctly.");

        var httpStatusCode = (HttpStatusCode)httpContext.Response.StatusCode;

        // Initialize the Http Response...
        var httpResponseData = httpRequestData.CreateResponse(httpStatusCode);

        // Marshall over all Headers from the HttpContext...
        // Note: This should also handle Cookies since Cookies are stored as a Header value ....
        var responseHeaders = httpContext.Response.Headers;

        if (responseHeaders.Count > 0)
        {
            foreach (var (key, value) in responseHeaders)
            {
                httpResponseData.Headers.TryAddWithoutValidation(
                    key,
                    value.Select(sv => sv?.ToString()));
            }
        }

        // Marshall the original response Bytes from HotChocolate...
        // Note: This enables full support for GraphQL Json results/errors,
        // binary downloads, SDL, & BCP binary data.
        var responseBytes = await httpContext.ReadResponseBytesAsync().ConfigureAwait(false);

        if (responseBytes != null)
        {
            await httpResponseData.WriteBytesAsync(responseBytes).ConfigureAwait(false);
        }

        return httpResponseData;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                HttpContext.DisposeSafely();
            }
            _disposed = true;
        }
    }
}
