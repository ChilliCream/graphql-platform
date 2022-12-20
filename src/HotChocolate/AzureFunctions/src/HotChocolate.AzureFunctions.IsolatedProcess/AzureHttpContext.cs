using System.Security.Claims;
using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace HotChocolate.AzureFunctions.IsolatedProcess;

internal sealed class AzureHttpContext : HttpContext
{
    private readonly DefaultHttpContext _innerContext;
    private readonly AzureHttpResponse _innerResponse;

    public AzureHttpContext(HttpRequestData requestData)
    {
        if (requestData is null)
        {
            throw new ArgumentNullException(nameof(requestData));
        }

        _innerContext = new DefaultHttpContext();
        _innerResponse = new AzureHttpResponse(_innerContext.Response, requestData);

        var contentType =
            requestData.Headers.TryGetValues(HeaderNames.ContentType, out var headerValue)
                ? headerValue.First()
                : ContentType.Json;

        _innerContext.User = new ClaimsPrincipal(requestData.Identities);

        var request = _innerContext.Request;
        request.Method = requestData.Method;
        request.Scheme = requestData.Url.Scheme;
        request.Host = new HostString(requestData.Url.Host, requestData.Url.Port);
        request.Path = new PathString(requestData.Url.AbsolutePath);
        request.QueryString = new QueryString(requestData.Url.Query);
        request.Body = requestData.Body;
        request.ContentType = contentType;

        foreach (var (key, value) in requestData.Headers)
        {
            request.Headers.TryAdd(key, new StringValues(value.ToArray()));
        }

        foreach(var (key, value) in requestData.FunctionContext.Items)
        {
            Items.Add(key, value);
        }
    }

    public override IFeatureCollection Features => _innerContext.Features;

    public override HttpRequest Request => _innerContext.Request;

    public override HttpResponse Response => _innerResponse;

    public override ConnectionInfo Connection => _innerContext.Connection;

    public override WebSocketManager WebSockets => _innerContext.WebSockets;

    public override ClaimsPrincipal User
    {
        get => _innerContext.User;
        set => _innerContext.User = value;
    }

    public override IDictionary<object, object?> Items
    {
        get => _innerContext.Items;
        set => _innerContext.Items = value;
    }

    public override IServiceProvider RequestServices
    {
        get => _innerContext.RequestServices;
        set => _innerContext.RequestServices = value;
    }

    public override CancellationToken RequestAborted
    {
        get => _innerContext.RequestAborted;
        set => _innerContext.RequestAborted = value;
    }

    public override string TraceIdentifier
    {
        get => _innerContext.TraceIdentifier;
        set => _innerContext.TraceIdentifier = value;
    }

    public override ISession Session
    {
        get => _innerContext.Session;
        set => _innerContext.Session = value;
    }

    public override void Abort()
        => _innerContext.Abort();

    public HttpResponseData CreateResponseData()
        => _innerResponse.ResponseData;
}
