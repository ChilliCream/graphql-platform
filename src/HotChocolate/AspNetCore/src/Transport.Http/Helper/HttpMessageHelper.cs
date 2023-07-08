using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using HotChocolate.Transport.Abstractions;
using HotChocolate.Transport.Abstractions.Helpers;

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
        using var memoryStream = new MemoryStream();
        using var jsonWriter = new Utf8JsonWriter(memoryStream, JsonDefaults.WriterOptions);
        jsonWriter.WriteStartObject();

        if(request.Id is not null)
        {
            jsonWriter.WriteString("id", request.Id);
        }

        if(request.Query is not null)
        {
            jsonWriter.WriteString("query", request.Query);
        }

        if(request.OperationName is not null)
        {
            jsonWriter.WriteString("operationName", request.OperationName);
        }

        // if (request.ExtensionsNode is not null)
        // {
        //     jsonWriter.WritePropertyName("extensions");
        //     WriteFieldValue(jsonWriter, request.ExtensionsNode);
        // }
        // else if (request.Extensions is not null)
        // {
        //     jsonWriter.WritePropertyName(ExtensionsProp);
        //     WriteFieldValue(jsonWriter, request.Extensions);
        // }
        //
        // if (request.VariablesNode is not null)
        // {
        //     jsonWriter.WritePropertyName(VariablesProp);
        //     WriteFieldValue(jsonWriter, request.VariablesNode);
        // }
        // else if (request.Variables is not null)
        // {
        //     jsonWriter.WritePropertyName(VariablesProp);
        //     WriteFieldValue(jsonWriter, request.Variables);
        // }

        jsonWriter.WriteEndObject();

        jsonWriter.Flush();
        var json = Encoding.UTF8.GetString(memoryStream.ToArray());
        requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");
        return requestMessage;
    }
}
