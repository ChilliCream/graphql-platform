using System.Collections.Immutable;
using System.IO.Pipelines;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Net.Http.Headers;

namespace HotChocolate.AzureFunctions.IsolatedProcess;

internal sealed class AzureHttpResponse : HttpResponse
{
    private static readonly StreamPipeWriterOptions s_options = new(leaveOpen: true);
    private ImmutableList<(Func<object, Task>, object)> _onCompletedCallbacks = [];
    private readonly HttpResponse _response;
    private readonly HttpRequestData _requestData;
    private readonly object _sync = new();
    private HttpResponseData? _responseData;
    private AzureHeaderDictionary? _headers;
    private PipeWriter? _writer;

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
                    _responseData ??= _requestData.CreateResponse();
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
                    _headers ??= new AzureHeaderDictionary(_response, ResponseData);
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

    public override PipeWriter BodyWriter
    {
        get
        {
            _writer ??= PipeWriter.Create(Body, s_options);
            return _writer;
        }
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
    {
        lock (_sync)
        {
            _onCompletedCallbacks = _onCompletedCallbacks.Add((callback, state));
        }
    }

    public override void Redirect(string location, bool permanent)
        => throw new NotSupportedException();

    public override async Task CompleteAsync()
    {
        if (_writer is not null)
        {
            await _writer.FlushAsync().ConfigureAwait(false);
            await _writer.CompleteAsync().ConfigureAwait(false);
        }

        if (!_onCompletedCallbacks.IsEmpty)
        {
            foreach (var (callback, state) in _onCompletedCallbacks)
            {
                await callback(state).ConfigureAwait(false);
            }
        }
    }
}
