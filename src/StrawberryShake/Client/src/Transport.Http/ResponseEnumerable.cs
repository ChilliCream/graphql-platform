using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Transport.Http;
using StrawberryShake.Internal;
using static StrawberryShake.Properties.Resources;

namespace StrawberryShake.Transport.Http;

internal sealed class ResponseEnumerable : IAsyncEnumerable<Response<JsonDocument>>
{
    private readonly Func<HttpClient> _createClient;
    private readonly Func<GraphQLHttpRequest> _createRequest;

    private ResponseEnumerable(
        Func<HttpClient> createClient,
        Func<GraphQLHttpRequest> createRequest)
    {
        _createClient = createClient;
        _createRequest = createRequest;
    }

    public async IAsyncEnumerator<Response<JsonDocument>> GetAsyncEnumerator(
        CancellationToken cancellationToken = default)
    {
        using var client = new DefaultGraphQLHttpClient(_createClient());
        var request = _createRequest();

        var result = await client.SendAsync(request, cancellationToken);

        Exception? transportError = null;
        if (!result.IsSuccessStatusCode)
        {
            transportError = CreateError(result);
        }

        ConfiguredCancelableAsyncEnumerable<HotChocolate.Transport.OperationResult>.Enumerator
            enumerator = default!;
        try
        {
            enumerator = result
                .ReadAsResultStreamAsync(cancellationToken)
                .ConfigureAwait(false)
                .WithCancellation(cancellationToken)
                .GetAsyncEnumerator();
        }
        catch (Exception ex)
        {
            transportError ??= ex;
        }

        var hasNext = true;
        while (hasNext)
        {
            try
            {
                hasNext = await enumerator.MoveNextAsync();
            }
            catch (Exception ex)
            {
                hasNext = false;
                transportError ??= ex;
            }

            // in case we have a result we will still parse and return it even if we have a
            // transport error as we will continue to read the stream until the end.
            if (hasNext)
            {
                var parsedResult = ParseResult(enumerator.Current);

                yield return new Response<JsonDocument>(parsedResult, transportError);

                transportError = null;
            }
            else if (transportError is not null)
            {
                var errorBody = CreateBodyFromException(transportError);
                yield return new Response<JsonDocument>(errorBody, transportError);
            }
        }
    }

    private static JsonDocument? ParseResult(HotChocolate.Transport.OperationResult? result)
    {
        if (result is null)
        {
            return null;
        }

        var buffer = new HotChocolate.Utilities.ArrayWriter();
        using var writer = new Utf8JsonWriter(buffer);

        writer.WriteStartObject();
        WriteProperty(writer, "data", result.Data);

        // in case we have just a "Internal Execution Error" we will not write the errors as this
        // is a internal error of HotChocolate.Transport.Http. In strawberry shake we are used to
        // handle the transport errors our self.
        // Strawberry Shake only outputs the exceptions though if there is no error in the errors
        // field
        if (result.Errors.ValueKind is not JsonValueKind.Array ||
            result.Errors.GetArrayLength() != 1 ||
            !result.Errors[0].TryGetProperty("message", out var message) ||
            message.GetString() is not "Internal Execution Error")
        {
            WriteProperty(writer, "errors", result.Errors);
        }

        WriteProperty(writer, "extensions", result.Extensions);
        writer.WriteEndObject();

        writer.Flush();

        return JsonDocument.Parse(buffer.GetWrittenMemory());
    }

    private static void WriteProperty(Utf8JsonWriter writer, string propertyName, JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Undefined)
        {
            writer.WritePropertyName(propertyName);
            value.WriteTo(writer);
        }
    }

    private static Exception CreateError(GraphQLHttpResponse response)
    {
        return new HttpRequestException(
            string.Format(
                ResponseEnumerator_HttpNoSuccessStatusCode,
                (int)response.StatusCode,
                response.ReasonPhrase),
            null,
            response.StatusCode);
    }

    internal static JsonDocument CreateBodyFromException(Exception exception)
    {
        using var bufferWriter = new ArrayWriter();

        using var jsonWriter = new Utf8JsonWriter(bufferWriter);
        jsonWriter.WriteStartObject();
        jsonWriter.WritePropertyName("errors");
        jsonWriter.WriteStartArray();
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString("message", exception.Message);
        jsonWriter.WriteEndObject();
        jsonWriter.WriteEndArray();
        jsonWriter.WriteEndObject();
        jsonWriter.Flush();

        return JsonDocument.Parse(bufferWriter.GetWrittenMemory());
    }

    public static ResponseEnumerable Create(
        Func<HttpClient> createClient,
        Func<GraphQLHttpRequest> createRequest)
    {
        if (createClient is null)
        {
            throw new ArgumentNullException(nameof(createClient));
        }

        if (createRequest is null)
        {
            throw new ArgumentNullException(nameof(createRequest));
        }

        return new ResponseEnumerable(createClient, createRequest);
    }
}
