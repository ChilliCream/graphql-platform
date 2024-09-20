using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace HotChocolate.AzureFunctions.IsolatedProcess.Tests.Helpers;

public class MockHttpResponseData : HttpResponseData, IDisposable
{
    public MockHttpResponseData(FunctionContext functionContext)
        : base(functionContext)
    { }

    public override HttpStatusCode StatusCode { get; set; }

    public override HttpHeadersCollection Headers { get; set; } = [];

    public override Stream Body { get; set; } = new MemoryStream();

    public override HttpCookies Cookies { get; } = new MockHttpResponseDataCookies();

    public void Dispose()
    {
        Body.Dispose();
    }
}

/// <summary>
/// NOTE: Since HttpCookies base class doesn't offer any way to actually read the cookies, we
/// don't even worry about populating them, we just need the Mock for basic testing...
/// </summary>
public class MockHttpResponseDataCookies : HttpCookies
{
    private readonly Dictionary<string, IHttpCookie> _cookieJar = new();

    public override void Append(string name, string value)
        => _cookieJar.TryAdd(name, new HttpCookie(name, value));

    public override void Append(IHttpCookie cookie)
        => _cookieJar.TryAdd(cookie.Name, cookie);

    public override IHttpCookie CreateNew()
        => new HttpCookie(string.Empty, string.Empty);
}
