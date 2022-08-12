using System;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AzureFunctions.IsolatedProcess.Extensions;
public static class HttpContextExtensions
{
    public static async Task<byte[]?> ReadResponseBytesAsync(this HttpContext httpContext)
    {
        Stream? responseStream = httpContext?.Response?.Body;
        switch (responseStream)
        {
            case null:
                return null;
            case MemoryStream alreadyMemoryStream:
                return alreadyMemoryStream.ToArray();
            default:
                await using (var memoryStream = new MemoryStream())
                {
                    await responseStream.CopyToAsync(memoryStream).ConfigureAwait(false);
                    return memoryStream.ToArray();
                }
        }
    }

    public static async Task<string?> ReadStreamAsStringAsync(this Stream responseStream)
    {
        string? responseContent = null;
        var originalPosition = responseStream.Position;

        if (responseStream.CanSeek)
            responseStream.Seek(0, SeekOrigin.Begin);

        using (var responseReader = new StreamReader(responseStream))
            responseContent = await responseReader.ReadToEndAsync().ConfigureAwait(false);

        if (responseStream.CanSeek)
            responseStream.Seek(originalPosition, SeekOrigin.Begin);

        return responseContent;
    }

    public static async Task<string?> ReadResponseContentAsync(this HttpContext httpContext)
    {
        Stream? responseStream = httpContext?.Response?.Body;
        return responseStream != null
            ? await responseStream.ReadStreamAsStringAsync().ConfigureAwait(false)
            : null;
    }

    public static Uri GetAbsoluteUri(this HttpRequest httpRequest)
    {
        var urlBuilder = new UriBuilder(httpRequest.Scheme, httpRequest.Host.Host);

        if (httpRequest.Host.Port != null)
            urlBuilder.Port = (int)httpRequest.Host.Port;

        urlBuilder.Path = httpRequest.Path.Value;
        urlBuilder.Query = httpRequest.QueryString.Value;

        return urlBuilder.Uri;
    }

    public static void DisposeSafely(this HttpContext httpContext)
    {
        httpContext?.Request?.Body?.Dispose();
        httpContext?.Response?.Body?.Dispose();
    }
}
