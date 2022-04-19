using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Primitives;

namespace HotChocolate.AzureFunctions.IsolatedProcess
{
    public class GraphQLIsolatedProcessHttpContextShim : IDisposable, IAsyncDisposable
    {
        //Must keep the Reference so we can safely Dispose!
        protected HttpContext? HttpContextShim { get; set; }

        public HttpRequestData IsolatedProcessHttpRequestData { get; protected set; }

        public string ContentType { get; protected set; }

        protected bool IsDisposed { get; set; } = false;

        public GraphQLIsolatedProcessHttpContextShim(HttpRequestData httpRequestData)
        {
            IsolatedProcessHttpRequestData = httpRequestData ?? throw new ArgumentNullException(nameof(httpRequestData));
            ContentType = httpRequestData.GetContentType();
        }

        /// <summary>
        /// Create an HttpContext (AspNetCore compatible) that can be provided to the AzureFunctionsProxy for GraphQL execution.
        /// All pertinent data from the HttpRequestData provided by the Azure Functions Isolated Process will be marshalled
        /// into the HttpContext for HotChocolate to consume.
        /// </summary>
        /// <returns></returns>
        public virtual async Task<HttpContext> CreateGraphQLHttpContextAsync()
        {
            HttpRequestData httpRequestData = IsolatedProcessHttpRequestData;

            HttpContext httpContextShim = BuildGraphQLHttpContext(
                requestHttpMethod: httpRequestData.Method,
                requestUri: httpRequestData.Url,
                requestHeadersCollection: httpRequestData.Headers,
                requestBody: await httpRequestData.ReadAsStringAsync().ConfigureAwait(false),
                requestBodyContentType: httpRequestData.GetContentType(),
                claimsIdentities: httpRequestData.Identities
            );

            //Ensure we track the HttpContext internally for cleanup when disposed!
            HttpContextShim = httpContextShim;
            return httpContextShim;
        }

        // ReSharper disable once InconsistentNaming
        protected virtual HttpContext BuildGraphQLHttpContext(
            string requestHttpMethod,
            Uri requestUri,
            HttpHeadersCollection requestHeadersCollection,
            string? requestBody = null,
            string requestBodyContentType = GraphQLAzureFunctionsConstants.DefaultJsonContentType,
            IEnumerable<ClaimsIdentity>? claimsIdentities = null
        )
        {
            //Initialize the root Http Context (Container)...
            var httpContext = new DefaultHttpContext();

            //Initialize the Http Request...
            HttpRequest httpRequest = httpContext.Request;
            httpRequest.Scheme = requestUri.Scheme;
            httpRequest.Path = new PathString(requestUri.AbsolutePath);
            httpRequest.Method = requestHttpMethod ?? HttpMethod.Post.Method;
            httpRequest.QueryString = new QueryString(requestUri.Query);

            //Ensure we marshall across all Headers from the Client Request...
            if (requestHeadersCollection?.Any() == true)
                foreach(var header in requestHeadersCollection)
                    httpRequest.Headers.Add(header.Key, new StringValues(header.Value.ToArray()));
            
            if (!string.IsNullOrEmpty(requestBody))
            {
                //Initialize a valid Stream for the Request (must be tracked & Disposed of!)
                var requestBodyBytes = Encoding.UTF8.GetBytes(requestBody);
                httpRequest.Body = new MemoryStream(requestBodyBytes);
                httpRequest.ContentType = requestBodyContentType;
                httpRequest.ContentLength = requestBodyBytes.Length;
            }

            //Initialize the Http Response...
            HttpResponse httpResponse = httpContext.Response;
            //Initialize a valid Stream for the Response (must be tracked & Disposed of!)
            //NOTE: Default Body is a NullStream...which ignores all Reads/Writes.
            httpResponse.Body = new MemoryStream();

            //Proxy over any possible authentication claims if available
            if (claimsIdentities?.Any() == true)
                httpContext.User = new ClaimsPrincipal(claimsIdentities);

            return httpContext;
        }

        /// <summary>
        /// Create an HttpResponseData containing the proxied GraphQL results; marshalled back from
        /// the HttpContext that HotChocolate populates.
        /// </summary>
        /// <returns></returns>
        public async Task<HttpResponseData> CreateHttpResponseDataAsync()
        {
            var graphqlResponseBytes = ReadResponseBytes();
            HttpContext? httpContext = HttpContextShim ?? throw new ArgumentNullException(nameof(HttpContextShim), "The HttpContext has not been initialized correctly.");

            var httpStatusCode = (HttpStatusCode)httpContext.Response.StatusCode;

            //Initialize the Http Response...
            HttpRequestData httpRequestData = IsolatedProcessHttpRequestData;
            HttpResponseData response = httpRequestData.CreateResponse(httpStatusCode);

            //Marshall over all Headers from the HttpContext...
            //Note: This should also handle Cookies (not tested)....
            IHeaderDictionary? responseHeaders = httpContext.Response?.Headers;
            if (responseHeaders?.Any() == true)
                foreach (var header in responseHeaders)
                    response.Headers.Add(header.Key, header.Value.Select(sv => sv.ToString()));

            //Marshall the original response Bytes from HotChocolate...
            //Note: This enables full support for GraphQL Json results/errors, binary downloads, SDL, & BCP binary data.
            if(graphqlResponseBytes != null)
                await response.WriteBytesAsync(graphqlResponseBytes).ConfigureAwait(false);

            return response;
        }

        public byte[]? ReadResponseBytes()
        {
            if (HttpContextShim?.Response?.Body is not MemoryStream responseMemoryStream) 
                return null;
            
            var bytes = responseMemoryStream.ToArray();
            return bytes;

        }

        public virtual string? ReadResponseContentAsString()
        {
            var responseBytes = ReadResponseBytes();

            return responseBytes != null
                ? Encoding.UTF8.GetString(responseBytes)
                : string.Empty;
        }

        public ValueTask DisposeAsync()
        {
            if(!IsDisposed) Dispose();
            return ValueTask.CompletedTask;
        }

        public virtual void Dispose()
        {
            if (IsDisposed) return;

            HttpContextShim?.Request?.Body?.Dispose();
            HttpContextShim?.Response?.Body?.Dispose();
            HttpContextShim = null;

            GC.SuppressFinalize(this);
        }
    }
}
