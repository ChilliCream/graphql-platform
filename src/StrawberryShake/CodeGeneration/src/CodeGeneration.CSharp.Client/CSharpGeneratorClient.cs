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
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly Encoding _headerEncoding = Encoding.ASCII;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Stream _requestStream;
    private readonly Stream _responseStream;
    private readonly byte[] _buffer = new byte[1024];

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

            var response = await ReadResponseInternalAsync();
            return JsonSerializer.Deserialize<GeneratorResponse>(response)!;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<string> ReadResponseInternalAsync()
    {
        using var response = new MemoryStream();
        int read;

        do
        {
            read = await _responseStream.ReadAsync(_buffer, default);
            if (read > 0)
            {
                await response.WriteAsync(_buffer, 0, read);
            }
        } while (read == _buffer.Length);

        response.Seek(0, SeekOrigin.Begin);

        using var responseReader = new StreamReader(response);

        string? line;
        do
        {
            line = await responseReader.ReadLineAsync();
        } while (!string.IsNullOrEmpty(line));

        return await responseReader.ReadToEndAsync();
    }

    private readonly struct GeneratorRequestMessage
    {
        public GeneratorRequestMessage(GeneratorRequest request) : this()
        {
            Params = request;
        }

        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; } = "2.0";

        [JsonPropertyName("method")]
        public string Method { get; } = "generator/Generate";

        [JsonPropertyName("id")]
        public int Id { get; } = 1;

        [JsonPropertyName("params")]
        public GeneratorRequest Params { get; }
    }
}
