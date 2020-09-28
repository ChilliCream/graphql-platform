using System.Buffers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Stitching.Utilities
{
    internal class HttpQueryClient
    {
        private static readonly KeyValuePair<string, string> _contentTypeJson =
            new KeyValuePair<string, string>("Content-Type", "application/json");

        private static readonly JsonSerializerOptions _jsonSerializerOptions =
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IgnoreNullValues = true,
                IgnoreReadOnlyProperties = false
            };

        private static readonly JsonWriterOptions _jsonWriterOptions =
            new JsonWriterOptions
            {
                SkipValidation = true,
                Indented = false
            };

        public async Task<IReadOnlyQueryResult> FetchAsync(
            IReadOnlyQueryRequest request,
            HttpClient httpClient,
            IEnumerable<IHttpQueryRequestInterceptor>? interceptors = default,
            CancellationToken cancellationToken = default)
        {
            using var writer = new ArrayWriter();
            using var jsonWriter = new Utf8JsonWriter(writer, _jsonWriterOptions);
            WriteJsonRequest(writer, jsonWriter, request);
            jsonWriter.Flush();

            var requestBody = new ByteArrayContent(writer.GetInternalBuffer(), 0, writer.Length);
            requestBody.Headers.Add(_contentTypeJson.Key, _contentTypeJson.Value);

            // note: this one has to be awaited since byte array content uses a rented buffer
            // which is released as soon as writer is disposed.
            return await FetchAsync(
                request,
                requestBody,
                httpClient,
                interceptors,
                cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<IReadOnlyQueryResult> FetchAsync(
            IReadOnlyQueryRequest request,
            HttpContent requestContent,
            HttpClient httpClient,
            IEnumerable<IHttpQueryRequestInterceptor>? interceptors,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage message =
                await FetchInternalAsync(requestContent, httpClient).ConfigureAwait(false);

            using (Stream stream = await message.Content.ReadAsStreamAsync().ConfigureAwait(false))
            {
                object response = await BufferHelper.ReadAsync(
                    stream,
                    (buffer, bytesBuffered) => ParseJson(buffer, bytesBuffered),
                    cancellationToken)
                    .ConfigureAwait(false);

                IReadOnlyQueryResult queryResult =
                    response is IReadOnlyDictionary<string, object> d
                        ? HttpResponseDeserializer.Deserialize(d)
                        : QueryResultBuilder.CreateError(
                            ErrorBuilder.New()
                                .SetMessage("Could not deserialize query response.")
                                .Build());

                if (interceptors is { })
                {
                    foreach (IHttpQueryRequestInterceptor interceptor in interceptors)
                    {
                        queryResult = await interceptor.OnResponseReceivedAsync(
                            request, message, queryResult)
                            .ConfigureAwait(false);
                    }
                }

                return queryResult;
            }
        }

        private static object? ParseJson(byte[] buffer, int bytesBuffered)
        {
            var json = new ReadOnlySpan<byte>(buffer, 0, bytesBuffered);
            return Utf8GraphQLRequestParser.ParseJson(json);
        }

        public Task<(string, HttpResponseMessage)> FetchStringAsync(
            HttpQueryRequest request,
            HttpClient httpClient)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (httpClient == null)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }

            return FetchStringInternalAsync(request, httpClient);
        }

        private async Task<(string, HttpResponseMessage)> FetchStringInternalAsync(
            HttpQueryRequest request,
            HttpClient httpClient)
        {
            byte[] json = JsonSerializer.SerializeToUtf8Bytes(request, _jsonSerializerOptions);
            var content = new ByteArrayContent(json, 0, json.Length);
            content.Headers.Add(_contentTypeJson.Key, _contentTypeJson.Value);

            HttpResponseMessage response =
                await httpClient.PostAsync(httpClient.BaseAddress, content)
                    .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            string responseContent = await response.Content.ReadAsStringAsync()
                .ConfigureAwait(false);

            return (responseContent, response);
        }

        private static async Task<HttpResponseMessage> FetchInternalAsync(
            HttpContent requestContent,
            HttpClient httpClient)
        {
            HttpResponseMessage response =
                await httpClient.PostAsync(default(Uri), requestContent)
                    .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return response;
        }

        private void WriteJsonRequest(
            IBufferWriter<byte> writer,
            Utf8JsonWriter jsonWriter,
            IReadOnlyQueryRequest request)
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WriteString("query", request.Query.ToString());

            if (request.OperationName is { })
            {
                jsonWriter.WriteString("operationName", request.OperationName);
            }

            WriteJsonRequestVariables(writer, jsonWriter, request.VariableValues);
            jsonWriter.WriteEndObject();
        }

        private static void WriteJsonRequestVariables(
            IBufferWriter<byte> writer,
            Utf8JsonWriter jsonWriter,
            IReadOnlyDictionary<string, object> variables)
        {
            if (variables is { } && variables.Count > 0)
            {
                jsonWriter.WritePropertyName("variables");

                jsonWriter.WriteStartObject();

                foreach (KeyValuePair<string, object> variable in variables)
                {
                    jsonWriter.WritePropertyName(variable.Key);
                    WriteValue(writer, jsonWriter, variable.Value);
                }

                jsonWriter.WriteEndObject();
            }
        }

        private static void WriteValue(
            IBufferWriter<byte> writer,
            Utf8JsonWriter jsonWriter,
            object value)
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
                        jsonWriter.WriteStringValue(i.Value);
                        RemoveQuotes(writer, jsonWriter, i.Value.Length);
                        break;

                    case FloatValueNode f:
                        jsonWriter.WriteStringValue(f.Value);
                        RemoveQuotes(writer, jsonWriter, f.Value.Length);
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

        private static void RemoveQuotes(IBufferWriter<byte> writer, Utf8JsonWriter jsonWriter, int length)
        {
            jsonWriter.Flush();
            writer.Advance(-(length + 2));
            Span<byte> span = writer.GetSpan(length + 2);
            span.Slice(1, length + 1).CopyTo(span);
            writer.Advance(length);
        }
    }
}
