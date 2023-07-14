using System.Net.Http;
using System.Text.Json;
using HotChocolate.Transport.Abstractions;
using HotChocolate.Transport.Abstractions.Helpers;
using HotChocolate.Utilities;

namespace HotChocolate.Transport.Http.Helper;

internal static class HttpMessageHelper
{
    public static HttpRequestMessage AddDefaultAcceptHeaders(this HttpRequestMessage requestMessage)
    {
        requestMessage.Headers.Add("Accept", "application/graphql-response+json; charset=utf-8");
        requestMessage.Headers.Add("Accept", "application/json; charset=utf-8");
        return requestMessage;
    }

    public static HttpRequestMessage AddJsonBody(
        this HttpRequestMessage requestMessage,
        OperationRequest request)
    {
        using var arrayWriter= new ArrayWriter();

        using var jsonWriter = new Utf8JsonWriter(arrayWriter, JsonDefaults.WriterOptions);
        jsonWriter.WriteOperationRequest(request);
        jsonWriter.Flush();
        requestMessage.Content =  new ByteArrayContent(arrayWriter.GetInternalBuffer(), 0, arrayWriter.Length);
        return requestMessage;
    }
}
