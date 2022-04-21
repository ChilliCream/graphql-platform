using System.Net;
using HotChocolate.AzureFunctions.IsolatedProcess.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;

namespace HotChocolate.AzureFunctions.IsolatedProcess
{
    public class HttpContextShim : IDisposable, IAsyncDisposable
    {
        protected HttpRequestData? IsolatedProcessHttpRequestData { get; set; }

        //Must keep the Reference so we can safely Dispose!
        public HttpContext HttpContext { get; protected set; }

        protected virtual bool IsDisposed { get; set; } = false;

        public HttpContextShim(HttpContext httpContext)
        {
            HttpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
            IsolatedProcessHttpRequestData = null;
        }

        public HttpContextShim(HttpContext httpContext, HttpRequestData? httpRequestData)
        {
            HttpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
            IsolatedProcessHttpRequestData = httpRequestData ?? throw new ArgumentNullException(nameof(httpRequestData));
        }

        /// <summary>
        /// Factory method to Create an HttpContext that is AspNetCore compatible.
        /// All pertinent data from the HttpRequestData provided by the Azure Functions Isolated Process will be marshalled
        /// into the HttpContext for HotChocolate to consume.
        /// NOTE: This is done as Factory method (and not in the Constructor) to support optimized Async reading of incoming Request Content/Stream.
        /// </summary>
        /// <returns></returns>
        public static async Task<HttpContextShim> CreateHttpContextAsync(HttpRequestData httpRequestData)
        {
            if (httpRequestData == null)
                throw new ArgumentNullException(nameof(httpRequestData));

            var requestBody = await httpRequestData.ReadAsStringAsync().ConfigureAwait(false);

            HttpContext httpContext = new HttpContextBuilder().CreateHttpContext(
                requestHttpMethod: httpRequestData.Method,
                requestUri: httpRequestData.Url,
                requestBody: requestBody,
                requestBodyContentType: httpRequestData.GetContentType(),
                requestHeaders: httpRequestData.Headers,
                claimsIdentities: httpRequestData.Identities
            );

            //Ensure we track the HttpContext internally for cleanup when disposed!
            return new HttpContextShim(httpContext);
        }

        /// <summary>
        /// Create an HttpResponseData containing the proxied response content results; marshalled back from the HttpContext.
        /// </summary>
        /// <returns></returns>
        public async Task<HttpResponseData> CreateHttpResponseDataAsync()
        {
            HttpContext httpContext = HttpContext
                ?? throw new NullReferenceException("The HttpContext has not been initialized correctly.");

            HttpRequestData httpRequestData = IsolatedProcessHttpRequestData
                ?? throw new NullReferenceException("The HttpRequestData has not been initialized correctly.");

            var httpStatusCode = (HttpStatusCode)httpContext.Response.StatusCode;

            //Initialize the Http Response...
            HttpResponseData response = httpRequestData.CreateResponse(httpStatusCode);

            //Marshall over all Headers from the HttpContext...
            //Note: This should also handle Cookies (not tested)....
            IHeaderDictionary? responseHeaders = httpContext.Response?.Headers;
            if (responseHeaders?.Any() == true)
                foreach (var header in responseHeaders)
                    response.Headers.Add(header.Key, header.Value.Select(sv => sv.ToString()));

            //Marshall the original response Bytes from HotChocolate...
            //Note: This enables full support for GraphQL Json results/errors, binary downloads, SDL, & BCP binary data.
            var responseBytes = await httpContext.ReadResponseBytesAsync();
            if (responseBytes != null)
                await response.WriteBytesAsync(responseBytes).ConfigureAwait(false);

            return response;
        }

        public virtual ValueTask DisposeAsync()
        {
            if(!IsDisposed) Dispose();
            return ValueTask.CompletedTask;
        }

        public virtual void Dispose()
        {
            if (IsDisposed) return;

            HttpContext?.Request?.Body?.Dispose();
            HttpContext?.Response?.Body?.Dispose();
            HttpContext = null;

            GC.SuppressFinalize(this);
        }
    }
}
