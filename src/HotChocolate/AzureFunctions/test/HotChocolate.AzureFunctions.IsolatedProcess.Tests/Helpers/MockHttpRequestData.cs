using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Net.Http.Headers;
using static Microsoft.Net.Http.Headers.HeaderNames;

namespace HotChocolate.AzureFunctions.IsolatedProcess.Tests.Helpers;

public sealed class MockHttpRequestData : HttpRequestData, IDisposable
{
    private readonly List<IHttpCookie> _cookiesList = [];

    public MockHttpRequestData(
        FunctionContext functionContext,
        string requestHttpMethod,
        Uri requestUri,
        string? requestBody = null,
        string requestBodyContentType = TestConstants.DefaultJsonContentType,
        HttpHeaders? requestHeaders = null,
        IEnumerable<ClaimsIdentity>? claimsIdentities = null)
        : base(functionContext)
    {
        Method = requestHttpMethod ?? throw new ArgumentNullException(nameof(requestHttpMethod));
        Url = requestUri ?? throw new ArgumentNullException(nameof(requestUri));

        if(claimsIdentities != null)
        {
            Identities = claimsIdentities;
        }

        if (!string.IsNullOrEmpty(requestBody))
        {
            // Initialize a valid Stream for the Request (must be tracked & Disposed of!)
            var requestBodyBytes = Encoding.UTF8.GetBytes(requestBody);
            Body = new MemoryStream(requestBodyBytes);
            Headers.TryAddWithoutValidation(ContentType, requestBodyContentType);
            Headers.TryAddWithoutValidation(ContentLength, requestBodyBytes.Length.ToString());
        }

        // Ensure we marshall across all Headers from the HttpContext provided...
        if (requestHeaders?.Any() == true)
        {
            foreach (var (key, values) in requestHeaders)
            {
                Headers.TryAddWithoutValidation(key, values);
            }
        }

        // Because we are mocking this manually we must handle cookies explicitly...
        if (Headers.TryGetValues(Cookie, out var cookieHeaders))
        {
            var parsedCookieHeaders = CookieHeaderValue.ParseList(cookieHeaders.ToList());
            _cookiesList.AddRange(parsedCookieHeaders.Select(
                h => new HttpCookie(h.Name.ToString(), h.Value.ToString())));
        }
    }

    public override Stream Body { get; } = new MemoryStream();

    public override HttpHeadersCollection Headers { get; } = [];

    public override IReadOnlyCollection<IHttpCookie> Cookies => _cookiesList.AsReadOnly();

    public override Uri Url { get; }

    public override IEnumerable<ClaimsIdentity> Identities { get; } = [];

    public override string Method { get; }

    public override HttpResponseData CreateResponse() => new MockHttpResponseData(FunctionContext);

    public void Dispose()
    {
        Body.Dispose();
    }
}
