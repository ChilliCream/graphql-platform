using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp;

public class CSharpGeneratorClient
{
    private static readonly JsonSerializerOptions _options =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    private static readonly Encoding _headerEncoding = Encoding.ASCII;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Stream _requestStream;
    private readonly Stream _responseStream;

    public CSharpGeneratorClient(Stream requestStream, Stream responseStream)
    {
        _requestStream = requestStream;
        _responseStream = responseStream;
    }

    public async Task<GeneratorResponse> GenerateAsync(
        GeneratorRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);
            var message = JsonSerializer.SerializeToUtf8Bytes(
                new GeneratorRequestMessage(request),
                _options);

            var header = $"Content-Length: {message.Length}\r\n" +
                         "Content-Type: text/plain; charset=utf-8\r\n\r\n";
            var headerBytes = _headerEncoding.GetBytes(header);
            await _requestStream.WriteAsync(headerBytes, 0, headerBytes.Length, cancellationToken);
            await _requestStream.WriteAsync(message, 0, message.Length, cancellationToken);

            var response = await ReadResponseInternalAsync(cancellationToken);

            return JsonSerializer.Deserialize<GeneratorResponseMessage>(response, _options)!.Result;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<byte[]> ReadResponseInternalAsync(
        CancellationToken cancellationToken = default)
    {
        const byte r = (byte)'\r';
        const byte n = (byte)'\n';

        using var header = new MemoryStream();
        var receiveBuffer = new byte[1];

        int read;
        var lines = 0;
        var set = false;

        do
        {
            read = await _responseStream.ReadAsync(receiveBuffer, 0, 1, default);

            if (read > 0)
            {
                CheckNewLine(receiveBuffer[0]);
                header.WriteByte(receiveBuffer[0]);

                if (lines == 2)
                {
                    break;
                }
            }
        } while (read > 0);

        header.Seek(0, SeekOrigin.Begin);
        using var headerReader = new StreamReader(header);
        string? line;
        int? contentLength = null;

        do
        {
            line = await headerReader.ReadLineAsync();
            if (line is not null && line.StartsWith("Content-Length"))
            {
                contentLength = int.Parse(line.Split(':')[1].Trim());
                break;
            }
        } while (!string.IsNullOrEmpty(line));

        if (contentLength is null)
        {
            throw new Exception("Unable to read the message.");
        }

        const int maxRead = 256;
        var response = new byte[contentLength.Value];
        var pos = 0;
        var consumed = 0;

        do
        {
            var next = contentLength.Value - consumed;

            if (next > maxRead)
            {
                next = maxRead;
            }

            read = await _responseStream.ReadAsync(response, consumed, next, cancellationToken);
            consumed += read;
        } while (consumed < contentLength.Value);

        return response;

        void CheckNewLine(int b)
        {
            if (set)
            {
                if (b == n)
                {
                    lines++;
                    set = false;
                }
            }
            else
            {
                if (b == r)
                {
                    set = true;
                }
                else
                {
                    set = false;
                }
            }
        }
    }

    private readonly struct GeneratorRequestMessage
    {
        public GeneratorRequestMessage(GeneratorRequest request) : this()
        {
            Params = new(request);
        }

        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; } = "2.0";

        [JsonPropertyName("method")]
        public string Method { get; } = "generator/Generate";

        [JsonPropertyName("id")]
        public int Id { get; } = 1;

        [JsonPropertyName("params")]
        public GeneratorRequestMessageParams Params { get; }
    }

    private readonly struct GeneratorRequestMessageParams
    {
        public GeneratorRequestMessageParams(GeneratorRequest request)
        {
            Request = request;
        }

        [JsonPropertyName("request")]
        public GeneratorRequest Request { get; }
    }

    private class GeneratorResponseMessage
    {
        public GeneratorResponseMessage(GeneratorResponse result)
        {
            Result = result;
        }

        [JsonPropertyName("result")]
        public GeneratorResponse Result { get; }
    }
}
