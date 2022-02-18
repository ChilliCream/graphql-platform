using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Stitching.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching.Pipeline;

internal static class RemoteRequestHelper
{
    private static readonly (string Key, string Value) _contentType =
        ("Content-Type", "application/json; charset=utf-8");

    private static readonly JsonWriterOptions _jsonWriterOptions =
        new()
        {
            SkipValidation = true,
            Indented = false
        };

    public static async ValueTask<IQueryResult> ParseResponseMessageAsync(
        HttpResponseMessage responseMessage,
        CancellationToken cancellationToken)
    {
#if NET5_0_OR_GREATER
        await using Stream stream = await responseMessage.Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
#else
        using Stream stream = await responseMessage.Content
            .ReadAsStreamAsync()
            .ConfigureAwait(false);
#endif

        return await ParseResultAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    public static async ValueTask<IQueryResult> ParseErrorResponseMessageAsync(
        HttpResponseMessage responseMessage,
        CancellationToken cancellationToken)
    {
#if NET5_0_OR_GREATER
        await using Stream stream = await responseMessage.Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
#else
        using Stream stream = await responseMessage.Content
            .ReadAsStreamAsync()
            .ConfigureAwait(false);
#endif

        try
        {
            return await ParseResultAsync(stream, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            string? responseBody = null;

            if (stream.Length > 0)
            {
                var buffer = new byte[stream.Length];
                stream.Seek(0, SeekOrigin.Begin);
#if NET5_0_OR_GREATER
                var read = await stream.ReadAsync(buffer, cancellationToken)
                    .ConfigureAwait(false);
                responseBody = Encoding.UTF8.GetString(buffer, 0, read);
#else
                var read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                    .ConfigureAwait(false);
                responseBody = Encoding.UTF8.GetString(buffer, 0, read);
#endif
            }

            return QueryResultBuilder.CreateError(
                ErrorHelper.HttpRequestClient_HttpError(
                    responseMessage.StatusCode,
                    responseBody));
        }
    }

    public static async ValueTask<IQueryResult> ParseResultAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, object?> response =
            await BufferHelper.ReadAsync(
                    stream,
                    ParseResponse,
                    cancellationToken)
                .ConfigureAwait(false);

        return HttpResponseDeserializer.Deserialize(response);
    }

    private static IReadOnlyDictionary<string, object?> ParseResponse(
        byte[] buffer, int bytesBuffered) =>
        Utf8GraphQLRequestParser.ParseResponse(buffer.AsSpan(0, bytesBuffered))!;

    internal static async ValueTask<HttpRequestMessage> CreateRequestMessageAsync(
        ArrayWriter writer,
        IQueryRequest request,
        CancellationToken cancellationToken)
    {
        await using var jsonWriter = new Utf8JsonWriter(writer, _jsonWriterOptions);
        WriteJsonRequest(writer, jsonWriter, request);
        await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);

        return new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            Content = new ByteArrayContent(writer.GetInternalBuffer(), 0, writer.Length)
            {
                Headers = { { _contentType.Key, _contentType.Value } }
            }
        };
    }

    private static void WriteJsonRequest(
        ArrayWriter writer,
        Utf8JsonWriter jsonWriter,
        IQueryRequest request)
    {
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString("query", request.Query!.AsSpan());

        if (request.OperationName is not null)
        {
            jsonWriter.WriteString("operationName", request.OperationName);
        }

        WriteJsonRequestVariables(writer, jsonWriter, request.VariableValues);
        jsonWriter.WriteEndObject();
    }

    private static void WriteJsonRequestVariables(
        ArrayWriter writer,
        Utf8JsonWriter jsonWriter,
        IReadOnlyDictionary<string, object?>? variables)
    {
        if (variables?.Count > 0)
        {
            jsonWriter.WritePropertyName("variables");

            jsonWriter.WriteStartObject();

            foreach (KeyValuePair<string, object?> variable in variables)
            {
                jsonWriter.WritePropertyName(variable.Key);
                WriteValue(writer, jsonWriter, variable.Value);
            }

            jsonWriter.WriteEndObject();
        }
    }

    private static void WriteValue(
        ArrayWriter writer,
        Utf8JsonWriter jsonWriter,
        object? value)
    {
        if (value is null || value is NullValueNode)
        {
            jsonWriter.WriteNullValue();
        }
        else
        {
            switch (value)
            {
                case ObjectValueNode obj:
                    jsonWriter.WriteStartObject();

                    foreach (ObjectFieldNode field in obj.Fields)
                    {
                        jsonWriter.WritePropertyName(field.Name.Value);
                        WriteValue(writer, jsonWriter, field.Value);
                    }

                    jsonWriter.WriteEndObject();
                    break;

                case ListValueNode list:
                    jsonWriter.WriteStartArray();

                    foreach (IValueNode item in list.Items)
                    {
                        WriteValue(writer, jsonWriter, item);
                    }

                    jsonWriter.WriteEndArray();
                    break;

                case StringValueNode s:
                    jsonWriter.WriteStringValue(s.Value);
                    break;

                case EnumValueNode e:
                    jsonWriter.WriteStringValue(e.Value);
                    break;

                case IntValueNode i:
                    WriterNumber(i.AsSpan(), jsonWriter, writer);
                    break;

                case FloatValueNode f:
                    WriterNumber(f.AsSpan(), jsonWriter, writer);
                    break;

                case BooleanValueNode b:
                    jsonWriter.WriteBooleanValue(b.Value);
                    break;

                default:
                    throw new NotSupportedException(
                        StitchingResources.HttpRequestClient_UnknownVariableValueKind);
            }
        }
    }

    private static void WriterNumber(
        ReadOnlySpan<byte> number,
        Utf8JsonWriter jsonWriter,
        ArrayWriter arrayWriter)
    {
        jsonWriter.WriteNumberValue(0);
        jsonWriter.Flush();
        arrayWriter.GetInternalBuffer()[arrayWriter.Length - 1] = number[0];

        if (number.Length > 1)
        {
#if NET5_0_OR_GREATER
            number = number[1..];
#else
            number = number.Slice(1);
#endif
            Span<byte> span = arrayWriter.GetSpan(number.Length);
            number.CopyTo(span);
            arrayWriter.Advance(number.Length);
        }
    }
}
