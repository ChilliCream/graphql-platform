#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Parsers;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Buffers;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using static System.Net.HttpStatusCode;
using static HotChocolate.AspNetCore.Utilities.ErrorHelper;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;
using ThrowHelper = HotChocolate.AspNetCore.Utilities.ThrowHelper;

namespace HotChocolate.AspNetCore;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
public sealed class HttpMultipartMiddleware : HttpPostMiddlewareBase
{
    private const string Operations = "operations";
    private const string Map = "map";
    private static readonly JsonReaderOptions s_variablesReaderOptions =
        new()
        {
            CommentHandling = JsonCommentHandling.Skip
        };
    private static readonly JsonWriterOptions s_variableWriterOptions =
        new()
        {
            Indented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    private readonly FormOptions _formOptions;
    private readonly OperationResult _multipartRequestError = MultiPartRequestPreflightRequired();

    public HttpMultipartMiddleware(
        HttpRequestDelegate next,
        HttpRequestExecutorProxy executor,
        IOptions<FormOptions> formOptions)
        : base(next, executor)
    {
        ArgumentNullException.ThrowIfNull(formOptions);
        _formOptions = formOptions.Value;
    }

    public override async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method)
            && GetOptions(context).EnableMultipartRequests
            && context.ParseContentType() == RequestContentType.Form)
        {
            var session = await Executor.GetOrCreateSessionAsync(context.RequestAborted);

            if (!context.Request.Headers.ContainsKey(HttpHeaderKeys.Preflight)
                && GetOptions(context).EnforceMultipartRequestsPreflightHeader)
            {
                var headerResult = HeaderUtilities.GetAcceptHeader(context.Request);
                await session.WriteResultAsync(context, _multipartRequestError, headerResult.AcceptMediaTypes, BadRequest);
                return;
            }

            using (session.DiagnosticEvents.ExecuteHttpRequest(context, HttpRequestKind.HttpMultiPart))
            {
                await HandleRequestAsync(context, session, context.RequestAborted);
            }
        }
        else
        {
            // if the request is not a post multipart request or multipart requests are not enabled
            // we will just invoke the next middleware and do nothing:
            await NextAsync(context);
        }
    }

    protected override async ValueTask<GraphQLRequest[]> ParseRequestsFromBodyAsync(
        HttpContext context,
        ExecutorSession session)
    {
        IFormCollection? form;
        var httpRequest = context.Request;

        try
        {
            var formFeature = new FormFeature(httpRequest, _formOptions);
            form = await formFeature.ReadFormAsync(context.RequestAborted);
        }
        catch (Exception exception)
        {
            throw ThrowHelper.HttpMultipartMiddleware_Invalid_Form(exception);
        }

        // Parse the string values of interest from the IFormCollection
        var multipartRequest = ParseMultipartRequest(form);
        var requests = session.RequestParser.ParseRequest(multipartRequest.Operations);

        // we add the file lookup as a feature on the HttpContext and can grab it from
        // there and put it on the GraphQL request.
        context.Features.Set(multipartRequest.Files);

        for (var i = 0; i < requests.Length; i++)
        {
            var current = requests[i];

            context.Response.RegisterForDispose(current);

            if (!multipartRequest.FileMap.Root.TryGetNode(i.ToString(), out var operationRoot))
            {
                // Legacy multipart maps do not include an operation index.
                if (requests.Length == 1)
                {
                    operationRoot = multipartRequest.FileMap.Root;
                }
                else
                {
                    continue;
                }
            }

            if (current.Variables is null)
            {
                // the request is invalid as we have files for this request but no variables.
                throw new InvalidOperationException();
            }

            var json = JsonMarshal.GetRawUtf8Value(current.Variables.RootElement);
            var expectedBufferSize = json.Length + (json.Length / 5);
            var bufferWriter = new PooledArrayWriter(expectedBufferSize);
            var variablesReader = new Utf8JsonReader(json, s_variablesReaderOptions);
            await using var variablesWriter = new Utf8JsonWriter(bufferWriter, s_variableWriterOptions);

            try
            {
                RewriteVariables(ref variablesReader, variablesWriter, operationRoot);
                await variablesWriter.FlushAsync();

                current = current with
                {
                    Variables = JsonDocument.Parse(bufferWriter.WrittenMemory),
                    VariablesMemoryOwner = bufferWriter
                };
                context.Response.RegisterForDispose(current);

                requests[i] = current;
            }
            catch
            {
                bufferWriter.Dispose();

                foreach (var request in requests)
                {
                    request.Dispose();
                }

                throw;
            }
        }

        return requests;
    }

    private static HttpMultipartRequest ParseMultipartRequest(IFormCollection form)
    {
        string? operations = null;
        Dictionary<string, string[]>? map = null;

        foreach (var field in form)
        {
            if (field.Key == Operations)
            {
                if (!field.Value.TryPeek(out operations) || string.IsNullOrEmpty(operations))
                {
                    throw ThrowHelper.HttpMultipartMiddleware_No_Operations_Specified();
                }
            }
            else if (field.Key == Map)
            {
                if (string.IsNullOrEmpty(operations))
                {
                    throw ThrowHelper.HttpMultipartMiddleware_Fields_Misordered();
                }

                field.Value.TryPeek(out var mapString);

                try
                {
                    map = JsonSerializer.Deserialize(mapString!, FormsMapJsonContext.Default.Map);
                }
                catch
                {
                    throw ThrowHelper.HttpMultipartMiddleware_InvalidMapJson();
                }
            }
        }

        if (operations is null)
        {
            throw ThrowHelper.HttpMultipartMiddleware_No_Operations_Specified();
        }

        if (map is null)
        {
            throw ThrowHelper.HttpMultipartMiddleware_MapNotSpecified();
        }

        // Validate file mappings and bring them in an easy-to-use format
        var files = FormFileLookup.Create(map, form.Files);
        var fileMap = FileMapTrie.Parse(map);

        return new HttpMultipartRequest(operations, files, fileMap);
    }

    private void RewriteVariables(
        ref Utf8JsonReader originalVariables,
        Utf8JsonWriter variables,
        FileMapTrieNode fileMapRoot)
    {
        if (!originalVariables.Read())
        {
            throw new JsonException("The variables JSON payload is empty.");
        }

        RewriteJsonValue(ref originalVariables, variables, fileMapRoot);
    }

    private static void RewriteJsonValue(
        ref Utf8JsonReader reader,
        Utf8JsonWriter writer,
        FileMapTrieNode currentNode)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.StartObject:
                writer.WriteStartObject();
                while (reader.Read() && reader.TokenType is not JsonTokenType.EndObject)
                {
                    // Read property name, we allocate here the string as we have a string as key in our trie.
                    var propertyName = reader.GetString()!;
                    writer.WritePropertyName(propertyName);

                    // Try to navigate to the child node in the trie
                    var hasChildNode = currentNode.TryGetNode(propertyName, out var childNode);

                    // Read the property value
                    reader.Read();

                    // If this is a null, and we have a file key, replace it
                    if (reader.TokenType is JsonTokenType.Null
                        && hasChildNode
                        && childNode!.FileKey is not null)
                    {
                        writer.WriteStringValue(childNode.FileKey);
                    }
                    else if (hasChildNode)
                    {
                        // Recurse with the child node
                        RewriteJsonValue(ref reader, writer, childNode!);
                    }
                    else
                    {
                        // No mapping for this path, copy value as-is
                        CopyCurrentValue(ref reader, writer);
                    }
                }
                writer.WriteEndObject();
                break;

            case JsonTokenType.StartArray:
                writer.WriteStartArray();
                var index = 0;
                while (reader.Read() && reader.TokenType is not JsonTokenType.EndArray)
                {
                    // Try to navigate to the child node by array index
                    var indexKey = index.ToString();
                    var hasChildNode = currentNode.TryGetNode(indexKey, out var childNode);

                    // If this is a null, and we have a file key, replace it
                    if (reader.TokenType is JsonTokenType.Null
                        && hasChildNode
                        && childNode!.FileKey is not null)
                    {
                        writer.WriteStringValue(childNode.FileKey);
                    }
                    else if (hasChildNode)
                    {
                        // Recurse with the child node
                        RewriteJsonValue(ref reader, writer, childNode!);
                    }
                    else
                    {
                        // No mapping for this path, copy value as-is
                        CopyCurrentValue(ref reader, writer);
                    }

                    index++;
                }
                writer.WriteEndArray();
                break;

            default:
                // For all other token types (strings, numbers, booleans, null), copy as-is
                CopyCurrentValue(ref reader, writer);
                break;
        }
    }

    private static void CopyCurrentValue(ref Utf8JsonReader reader, Utf8JsonWriter writer)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.StartObject:
                writer.WriteStartObject();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        writer.WritePropertyName(reader.GetString()!);
                        reader.Read();
                        CopyCurrentValue(ref reader, writer);
                    }
                }
                writer.WriteEndObject();
                break;

            case JsonTokenType.StartArray:
                writer.WriteStartArray();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    CopyCurrentValue(ref reader, writer);
                }
                writer.WriteEndArray();
                break;

            case JsonTokenType.String:
                writer.WriteStringValue(reader.ValueSpan);
                break;

            case JsonTokenType.Number:
                writer.WriteRawValue(reader.ValueSpan);
                break;

            case JsonTokenType.True:
                writer.WriteBooleanValue(true);
                break;

            case JsonTokenType.False:
                writer.WriteBooleanValue(false);
                break;

            case JsonTokenType.Null:
                writer.WriteNullValue();
                break;
        }
    }
}

[JsonSerializable(typeof(Dictionary<string, string[]>), TypeInfoPropertyName = "Map")]
internal partial class FormsMapJsonContext : JsonSerializerContext;
