using System;
using System.Buffers;
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
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace HotChocolate.Stitching.Utilities
{
    internal class HttpQueryClient
    {
        private static readonly KeyValuePair<string, string> _contentTypeJson =
            new KeyValuePair<string, string>("Content-Type", "application/json");

        private readonly JsonSerializerSettings _jsonSettings =
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateParseHandling = DateParseHandling.None
            };

        public Task<QueryResult> FetchAsync(
            IReadOnlyQueryRequest request,
            HttpClient httpClient,
            IEnumerable<IHttpQueryRequestInterceptor> interceptors,
            CancellationToken cancellationToken)
        {
            using var writer = new RequestWriter();
            WriteJsonRequest(writer, request);
            var content = new ByteArrayContent(writer.GetInternalBuffer(), 0, writer.Length);
            content.Headers.Add(_contentTypeJson.Key, _contentTypeJson.Value);

            return FetchAsync(
                request,
                content,
                httpClient,
                interceptors,
                cancellationToken);
        }

        private async Task<QueryResult> FetchAsync(
            IReadOnlyQueryRequest request,
            HttpContent requestContent,
            HttpClient httpClient,
            IEnumerable<IHttpQueryRequestInterceptor> interceptors,
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

                QueryResult queryResult =
                    response is IReadOnlyDictionary<string, object> d
                        ? HttpResponseDeserializer.Deserialize(d)
                        : QueryResult.CreateError(
                            ErrorBuilder.New()
                                .SetMessage("Could not deserialize query response.")
                                .Build());

                if (interceptors is { })
                {
                    foreach (IHttpQueryRequestInterceptor interceptor in interceptors)
                    {
                        await interceptor.OnResponseReceivedAsync(
                            request, message, queryResult)
                            .ConfigureAwait(false);
                    }
                }

                return queryResult;
            }
        }

        private static object ParseJson(byte[] buffer, int bytesBuffered)
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
            var content = new StringContent(
                SerializeRemoteRequest(request),
                Encoding.UTF8,
                _contentTypeJson.Value);

            HttpResponseMessage response =
                await httpClient.PostAsync(default(Uri), content)
                    .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync()
                .ConfigureAwait(false);

            return (json, response);
        }

        private async Task<HttpResponseMessage> FetchInternalAsync(
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
            RequestWriter writer,
            IReadOnlyQueryRequest request)
        {
            writer.WriteStartObject();
            writer.WriteQuery(request.Query);
            writer.WriteOperationName(request.OperationName);
            WriteJsonRequestVariables(writer, request.VariableValues);
            writer.WriteEndObject();
        }

        private void WriteJsonRequestVariables(
            RequestWriter writer,
            IReadOnlyDictionary<string, object> variables)
        {
            if (variables is { } && variables.Count > 0)
            {
                writer.WritePropertyName("variables");

                writer.WriteStartObject();

                foreach (KeyValuePair<string, object> variable in variables)
                {
                    writer.WritePropertyName(variable.Key);
                    WriteValue(writer, variable.Value);
                }

                writer.WriteEndObject();
            }
        }

        private static void WriteValue(RequestWriter writer, object value)
        {
            if (value is null || value is NullValueNode)
            {
                writer.WriteNullValue();
            }
            else
            {
                switch (value)
                {
                    case ObjectValueNode obj:
                        writer.WriteStartObject();

                        foreach (ObjectFieldNode field in obj.Fields)
                        {
                            writer.WritePropertyName(field.Name.Value);
                            WriteValue(writer, field.Value);
                        }

                        writer.WriteEndObject();
                        break;

                    case ListValueNode list:
                        writer.WriteStartArray();

                        foreach (IValueNode item in list.Items)
                        {
                            WriteValue(writer, item);
                        }

                        writer.WriteEndArray();
                        break;

                    case StringValueNode s:
                        writer.WriteStringValue(s.Value);
                        break;

                    case EnumValueNode e:
                        writer.WriteStringValue(e.Value);
                        break;

                    case IntValueNode i:
                        writer.WriteNumberValue(i.Value);
                        break;

                    case FloatValueNode f:
                        writer.WriteNumberValue(f.Value);
                        break;

                    case BooleanValueNode b:
                        writer.WriteBooleanValue(b.Value);
                        break;

                    default:
                        throw new NotSupportedException(
                            "Unknown variable value kind.");
                }
            }
        }

        private string SerializeRemoteRequest(
            HttpQueryRequest remoteRequest)
        {
            return JsonConvert.SerializeObject(
                remoteRequest, _jsonSettings);
        }
    }

    internal sealed class RequestWriter
        : IBufferWriter<byte>
        , IDisposable
    {
        private static readonly JsonWriterOptions _options =
            new JsonWriterOptions { SkipValidation = true };
        private readonly Utf8JsonWriter _writer;
        private byte[] _buffer;
        private int _capacity;
        private int _start;
        private bool _disposed;

        public RequestWriter()
        {
            _buffer = ArrayPool<byte>.Shared.Rent(1024);
            _capacity = _buffer.Length;
            _writer = new Utf8JsonWriter(this, _options);
        }

        public int Length => _start;

        public ReadOnlyMemory<byte> Body => _buffer.AsMemory().Slice(0, _start);

        public byte[] GetInternalBuffer() => _buffer;

        public void WriteStartObject()
        {
            _writer.WriteStartObject();
            _writer.Flush();
        }

        public void WriteEndObject()
        {
            _writer.WriteEndObject();
            _writer.Flush();
        }

        public void WriteStartArray()
        {
            _writer.WriteStartArray();
            _writer.Flush();
        }

        public void WriteEndArray()
        {
            _writer.WriteEndArray();
            _writer.Flush();
        }

        public void WriteQuery(IQuery query)
        {
            _writer.WriteString("query", query.ToSpan());
            _writer.Flush();
        }

        public void WriteOperationName(string operationName)
        {
            if (operationName is { })
            {
                _writer.WriteString("operationName", operationName);
                _writer.Flush();
            }
        }

        public void WritePropertyName(string name)
        {
            _writer.WritePropertyName(name);
            _writer.Flush();
        }

        public void WriteNullValue()
        {
            _writer.WriteNullValue();
            _writer.Flush();
        }

        public void WriteStringValue(string value)
        {
            _writer.WriteStringValue(value);
            _writer.Flush();
        }

        public void WriteBooleanValue(bool value)
        {
            _writer.WriteBooleanValue(value);
            _writer.Flush();
        }

        public void WriteNumberValue(string value)
        {
            Span<byte> span = GetSpan(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                span[i] = (byte)value[i];
            }

            Advance(value.Length);
        }

        public void Advance(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            _start += count;
            _capacity -= count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            var size = sizeHint < 1 ? 256 : sizeHint;
            EnsureBufferCapacity(size);
            return _buffer.AsMemory().Slice(_start, size);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            var size = sizeHint < 1 ? 256 : sizeHint;
            EnsureBufferCapacity(size);
            return _buffer.AsSpan().Slice(_start, size);
        }

        private void EnsureBufferCapacity(int size)
        {
            if (_capacity < size)
            {
                byte[] buffer = _buffer;
                _buffer = ArrayPool<byte>.Shared.Rent(size + _buffer.Length);
                _capacity += _buffer.Length;
                buffer.AsSpan().CopyTo(_buffer);
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public void Clear()
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = ArrayPool<byte>.Shared.Rent(1024);
            _capacity = _buffer.Length;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = Array.Empty<byte>();
                _disposed = true;
            }
        }
    }
}
