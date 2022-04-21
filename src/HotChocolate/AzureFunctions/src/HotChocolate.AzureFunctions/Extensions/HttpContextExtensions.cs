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
                    await responseStream.CopyToAsync(memoryStream);
                    return memoryStream.ToArray();
                }
        }
    }

    public static async Task<string?> ReadResponseContentAsync(this HttpContext httpContext)
    {
        string? responseContent = null;

        Stream? responseStream = httpContext?.Response?.Body;
        if (responseStream != null)
        {
            var originalPosition = responseStream.Position;
            if (responseStream.CanSeek)
                responseStream.Seek(0, SeekOrigin.Begin);

            using (var responseReader = new StreamReader(responseStream))
                responseContent = await responseReader.ReadToEndAsync();

            if (responseStream.CanSeek)
                responseStream.Seek(originalPosition, SeekOrigin.Begin);
        }

        return responseContent;
    }
}
