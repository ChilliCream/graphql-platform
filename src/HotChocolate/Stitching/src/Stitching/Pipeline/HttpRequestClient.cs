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
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Stitching.Pipeline
{
    internal class HttpRequestClient
    {
        private static readonly (string Key, string Value) _contentType =
            ("Content-Type", "application/json; charset=utf-8");

        private static readonly JsonWriterOptions _jsonWriterOptions =
            new JsonWriterOptions
            {
                SkipValidation = true,
                Indented = false
            };

        private readonly IHttpClientFactory _clientFactory;
        private readonly IErrorHandler _errorHandler;
        private readonly IHttpStitchingRequestInterceptor _requestInterceptor;

        public HttpRequestClient(
            IHttpClientFactory clientFactory,
            IErrorHandler errorHandler,
            IHttpStitchingRequestInterceptor requestInterceptor)
        {
            _clientFactory = clientFactory;
            _errorHandler = errorHandler;
            _requestInterceptor = requestInterceptor;
        }

        public async Task<IQueryResult> FetchAsync(
            IQueryRequest request,
            NameString targetSchema,
            CancellationToken cancellationToken = default)
        {
            using var writer = new ArrayWriter();

            HttpRequestMessage requestMessage =
                await CreateRequestAsync(writer, request, targetSchema, cancellationToken)
                    .ConfigureAwait(false);

            return await FetchAsync(
                request,
                requestMessage,
                targetSchema,
                cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<IQueryResult> FetchAsync(
            IQueryRequest request,
            HttpRequestMessage requestMessage,
            NameString targetSchema,
            CancellationToken cancellationToken)
        {
            try
            {
                using HttpClient httpClient = _clientFactory.CreateClient(targetSchema);

                using HttpResponseMessage responseMessage = await httpClient
                    .SendAsync(requestMessage, cancellationToken)
                    .ConfigureAwait(false);

                IQueryResult result =
                    responseMessage.IsSuccessStatusCode
                        ? await ParseResponseMessageAsync(responseMessage, cancellationToken)
                            .ConfigureAwait(false)
                        : await ParseErrorResponseMessageAsync(responseMessage, cancellationToken)
                            .ConfigureAwait(false);

                return await _requestInterceptor.OnReceivedResultAsync(
                    targetSchema,
                    request,
                    result,
                    responseMessage,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                IError error = _errorHandler.CreateUnexpectedError(ex)
                    .SetCode(ErrorCodes.Stitching.UnknownRequestException)
                    .Build();

                return QueryResultBuilder.CreateError(error);
            }
            finally
            {
                requestMessage.Dispose();
            }
        }

        internal static async ValueTask<HttpRequestMessage> CreateRequestMessageAsync(
            ArrayWriter writer,
            IQueryRequest request,
            CancellationToken cancellationToken)
        {
            await using var jsonWriter = new Utf8JsonWriter(writer, _jsonWriterOptions);

            WriteJsonRequest(writer, jsonWriter, request);
            await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);

            var requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                Content = new ByteArrayContent(writer.GetInternalBuffer(), 0, writer.Length)
                {
                    Headers = { { _contentType.Key, _contentType.Value } }
                }
            };

            return requestMessage;
        }

        private static async ValueTask<IQueryResult> ParseErrorResponseMessageAsync(
            HttpResponseMessage responseMessage,
            CancellationToken cancellationToken)
        {
            using Stream stream = await responseMessage.Content
                .ReadAsStreamAsync()
                .ConfigureAwait(false);

            try
            {
                return await ParseResponseMessageAsync(responseMessage, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                string? responseBody = null;

                if (stream.Length > 0)
                {
                    var buffer = new byte[stream.Length];
                    stream.Seek(0, SeekOrigin.Begin);
                    await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                        .ConfigureAwait(false);
                    responseBody = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                }

                return QueryResultBuilder.CreateError(
                    ErrorHelper.HttpRequestClient_HttpError(
                        responseMessage.StatusCode,
                        responseBody));
            }
        }

        internal static async ValueTask<IQueryResult> ParseResponseMessageAsync(
            HttpResponseMessage responseMessage,
            CancellationToken cancellationToken)
        {
            using Stream stream = await responseMessage.Content
                .ReadAsStreamAsync()
                .ConfigureAwait(false);

            IReadOnlyDictionary<string, object?> response =
                await BufferHelper.ReadAsync(
                    stream,
                    ParseResponse,
                    cancellationToken)
                    .ConfigureAwait(false);

            return HttpResponseDeserializer.Deserialize(response);
        }

        private async ValueTask<HttpRequestMessage> CreateRequestAsync(
            ArrayWriter writer,
            IQueryRequest request,
            NameString targetSchema,
            CancellationToken cancellationToken = default)
        {
            HttpRequestMessage requestMessage =
                await CreateRequestMessageAsync(writer, request, cancellationToken)
                .ConfigureAwait(false);

            await _requestInterceptor
                .OnCreateRequestAsync(targetSchema, request, requestMessage, cancellationToken)
                .ConfigureAwait(false);

            return requestMessage;
        }

        private static IReadOnlyDictionary<string, object?> ParseResponse(
            byte[] buffer, int bytesBuffered) =>
            Utf8GraphQLRequestParser.ParseResponse(buffer.AsSpan(0, bytesBuffered))!;

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
            if (variables is not null  && variables.Count > 0)
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
                            "Unknown variable value kind.");
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
                number = number.Slice(1);
                number.CopyTo(arrayWriter.GetSpan(number.Length));
            }
        }
    }
}
