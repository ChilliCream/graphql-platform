using Microsoft.AspNetCore.Http;

namespace HotChocolate.AzureFunctions.Tests.Helpers;

public static class HttpContextExtensions
{
    private static async Task<string?> ReadStreamAsStringAsync(this Stream responseStream)
    {
        string? responseContent;
        var originalPosition = responseStream.Position;

        if (responseStream.CanSeek)
        {
            responseStream.Seek(0, SeekOrigin.Begin);
        }

        using (var responseReader = new StreamReader(responseStream))
        {
            responseContent = await responseReader.ReadToEndAsync().ConfigureAwait(false);
        }

        if (responseStream.CanSeek)
        {
            responseStream.Seek(originalPosition, SeekOrigin.Begin);
        }

        return responseContent;
    }

    public static async Task<string?> ReadResponseContentAsync(this HttpContext? httpContext)
    {
        var responseStream = httpContext?.Response?.Body;
        return responseStream != null
            ? await responseStream.ReadStreamAsStringAsync().ConfigureAwait(false)
            : null;
    }
}
