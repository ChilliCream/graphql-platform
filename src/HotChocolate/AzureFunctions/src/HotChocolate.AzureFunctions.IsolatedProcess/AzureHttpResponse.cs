using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Net.Http.Headers;

namespace HotChocolate.AzureFunctions.IsolatedProcess;

internal sealed class AzureHttpResponse : HttpResponse
{
    private readonly HttpResponse _response;
    private readonly HttpRequestData _requestData;
    private readonly object _sync = new();
    private HttpResponseData? _responseData;
    private AzureHeaderDictionary? _headers;

    public AzureHttpResponse(HttpResponse response, HttpRequestData requestData)
    {
        _response = response;
        _requestData = requestData;
    }

    internal HttpResponseData ResponseData
    {
        get
        {
            if (_responseData is null)
            {
                lock (_sync)
                {
                    if (_responseData is null)
                    {
                        _responseData = _requestData.CreateResponse();
                    }
                }
            }

            return _responseData;
        }
    }

    public override HttpContext HttpContext => _response.HttpContext;

    public override int StatusCode
    {
        get => (int)ResponseData.StatusCode;
        set => ResponseData.StatusCode = (HttpStatusCode)value;
    }

    public override IHeaderDictionary Headers
    {
        get
        {
            if (_headers is null)
            {
                lock (_sync)
                {
                    if (_headers is null)
                    {
                        _headers = new AzureHeaderDictionary(_response, ResponseData);
                    }
                }
            }
            return _headers;
        }
    }

    public override Stream Body
    {
        get => ResponseData.Body;
        set => ResponseData.Body = value;
    }

    public override long? ContentLength
    {
        get => Headers.ContentLength;
        set => Headers.ContentLength = value;
    }

    public override string? ContentType
    {
        get => Headers[HeaderNames.ContentType];
        set => Headers[HeaderNames.ContentType] = value;
    }

    public override IResponseCookies Cookies => _response.Cookies;

    public override bool HasStarted => _response.HasStarted;

    public override void OnStarting(Func<object, Task> callback, object state)
        => throw new NotSupportedException();

    public override void OnCompleted(Func<object, Task> callback, object state)
        => throw new NotSupportedException();

    public override void Redirect(string location, bool permanent)
        => throw new NotSupportedException();
}
