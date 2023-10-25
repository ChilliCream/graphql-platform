using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.OpenApi.Models;
using static HotChocolate.OpenApi.Properties.OpenApiResources;

namespace HotChocolate.OpenApi.Helpers;

internal static class OperationResolverHelper
{
    private static readonly JsonDocument _successResult = JsonDocument.Parse(BoolSuccessResult);
    
    public static Func<IResolverContext, Task<JsonElement>> CreateResolverFunc(string clientName, Operation operation)
        => context => ResolveAsync(context, clientName, operation);

    private static async Task<JsonElement> ResolveAsync(
        IResolverContext resolverContext, 
        string clientName, 
        Operation operation)
    {
        var services = resolverContext.Services;
        var stringBuilderPool = services.GetRequiredService<ObjectPool<StringBuilder>>();
        var httpClient = services.GetRequiredService<IHttpClientFactory>().CreateClient(clientName);
        
        using var arrayWriter = new ArrayWriter();
        var sb = stringBuilderPool.Get();
        sb.Clear();
        
        var request = CreateRequest(resolverContext, operation, arrayWriter, sb);
        stringBuilderPool.Return(sb);
        
        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
        }
        
        var responseBuffer = await response.Content.ReadAsByteArrayAsync();
        return ParseResponse(operation, responseBuffer);
    }
    
    private static JsonElement ParseResponse(Operation operation, ReadOnlySpan<byte> response)
    {
        if (response.Length == 0 && operation.Response?.Reference is null)
        {
            return _successResult.RootElement;
        }
        
        var reader = new Utf8JsonReader(response, true, default);
        return JsonElement.ParseValue(ref reader);
    }

    private static HttpRequestMessage CreateRequest(
        IResolverContext resolverContext, 
        Operation operation,
        ArrayWriter arrayWriter,
        StringBuilder sb)
    {
        HttpContent? content = null;

        BuildPath(resolverContext, operation, sb);
        BuildQueryString(resolverContext, operation, sb);

        if (operation.RequestBody is not null)
        {
            var valueNode = resolverContext.ArgumentLiteral<IValueNode>(InputField);
            
            using var writer = new Utf8JsonWriter(arrayWriter);
            Utf8JsonWriterHelper.WriteValueNode(writer, valueNode);
            writer.Flush();
            
            content = new ByteArrayContent(arrayWriter.GetInternalBuffer(), 0, arrayWriter.Length);
            content.Headers.ContentType = new MediaTypeHeaderValue(JsonMediaType);
        }

        var request = new HttpRequestMessage(operation.Method, sb.ToString());

        if (content is not null)
        {
            request.Content = content;
        }

        return request;
    }

    private static void BuildPath(IResolverContext resolverContext, Operation operation, StringBuilder sb)
    {
        sb.Append(operation.Path);
        
        ref var parameter = ref operation.GetParameterRef();
        ref var end = ref Unsafe.Add(ref parameter, operation.Parameters.Count);

        while (Unsafe.IsAddressLessThan(ref parameter, ref end))
        {
            var pathValue = operation.Method == HttpMethod.Get
                ? resolverContext.ArgumentValue<string>(parameter.Name)
                : GetValueOfValueNode(
                    resolverContext.ArgumentLiteral<IValueNode>(InputField), 
                    parameter.Name);
            sb.Replace($"{{{parameter.Name}}}", pathValue);
            parameter = ref Unsafe.Add(ref parameter, 1)!;
        }
    }

    private static void BuildQueryString(IResolverContext resolverContext, Operation operation, StringBuilder sb)
    {
        ref var parameter = ref operation.GetParameterRef();
        ref var end = ref Unsafe.Add(ref parameter, operation.Parameters.Count);
        var next = false;

        while (Unsafe.IsAddressLessThan(ref parameter, ref end))
        {
            if (parameter.In == ParameterLocation.Query)
            {
                var pathValue = operation.Method == HttpMethod.Get
                    ? resolverContext.ArgumentValue<string>(parameter.Name)
                    : GetValueOfValueNode(
                        resolverContext.ArgumentLiteral<IValueNode>(InputField),
                        parameter.Name);

                if (next)
                {
                    sb.Append('&');
                }
                else
                {
                    sb.Append('?');    
                    next = true;
                }
                
                sb.Append(parameter.Name);
                sb.Append('=');
                sb.Append(pathValue);
            }

            parameter = ref Unsafe.Add(ref parameter, 1)!;
        }
    }

    private static string GetJsonValueOfInputNode(IValueNode valueNode)
    {
        using var arrayWriter = new ArrayWriter();
        using var writer = new Utf8JsonWriter(arrayWriter);
        Utf8JsonWriterHelper.WriteValueNode(writer, valueNode);
        writer.Flush();
        return Encoding.UTF8.GetString(arrayWriter.GetInternalBuffer(), 0, arrayWriter.Length);
    }

    private static string GetValueOfValueNode(IValueNode input, string fieldName)
    {
        var json = GetJsonValueOfInputNode(input);
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty(fieldName).GetRawText();
    }
}
