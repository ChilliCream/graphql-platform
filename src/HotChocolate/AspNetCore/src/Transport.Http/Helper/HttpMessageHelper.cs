using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using HotChocolate.Transport.Serialization;
using HotChocolate.Utilities;

namespace HotChocolate.Transport.Http.Helper;

internal static class HttpMessageHelper
{
    private const string _jsonMediaType = "application/json";
    private const string _graphqlMediaType = "application/graphql-response+json";

    public static HttpRequestMessage AddDefaultAcceptHeaders(this HttpRequestMessage requestMessage)
    {
        requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(_graphqlMediaType));
        requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(_jsonMediaType));
        return requestMessage;
    }

    public static HttpRequestMessage AddJsonBody(
        this HttpRequestMessage requestMessage,
        OperationRequest request)
    {
        using var arrayWriter= new ArrayWriter();

        using var jsonWriter = new Utf8JsonWriter(arrayWriter, JsonDefaults.WriterOptions);
        request.WriteTo(jsonWriter);
        jsonWriter.Flush();
        requestMessage.Content =  new ByteArrayContent(arrayWriter.GetInternalBuffer(), 0, arrayWriter.Length);
        requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(_jsonMediaType);
        return requestMessage;
    }
}
