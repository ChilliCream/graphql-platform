using System.Buffers;
using System.Collections.Immutable;
using System.Text.Json;

namespace HotChocolate.Adapters.OpenApi;

public static class OpenApiEndpointSettingsSerializer
{
    public static void Format(OpenApiEndpointDefinition endpoint, IBufferWriter<byte> writer)
    {
        using var jsonWriter = new Utf8JsonWriter(writer);

        jsonWriter.WriteStartObject();

        // jsonWriter.WriteString("formatVersion", archiveMetadata.FormatVersion.ToString());
        //
        // if (archiveMetadata.Endpoints.Length > 0)
        // {
        //     jsonWriter.WriteStartArray("endpoints");
        //     foreach (var endpoint in archiveMetadata.Endpoints)
        //     {
        //         jsonWriter.WriteStartObject();
        //         jsonWriter.WriteString("httpMethod", endpoint.HttpMethod);
        //         jsonWriter.WriteString("route", endpoint.Route);
        //         jsonWriter.WriteEndObject();
        //     }
        //     jsonWriter.WriteEndArray();
        // }
        //
        // if (archiveMetadata.Models.Length > 0)
        // {
        //     jsonWriter.WriteStartArray("models");
        //     foreach (var model in archiveMetadata.Models)
        //     {
        //         jsonWriter.WriteStringValue(model);
        //     }
        //     jsonWriter.WriteEndArray();
        // }

        jsonWriter.WriteEndObject();
        jsonWriter.Flush();
    }

    public static OpenApiEndpointDefinition Parse(ReadOnlyMemory<byte> data)
    {
        using var document = JsonDocument.Parse(data);
        var root = document.RootElement;

        return null!;

        // if (root.ValueKind is not JsonValueKind.Object)
        // {
        //     throw new JsonException("Invalid archive metadata format.");
        // }
        //
        // var formatVersionProp = root.GetProperty("formatVersion");
        // if (formatVersionProp.ValueKind is not JsonValueKind.String)
        // {
        //     throw new JsonException("The archive metadata must contain a formatVersion property.");
        // }
        //
        // var endpoints = ImmutableArray<OpenApiEndpointKey>.Empty;
        // if (root.TryGetProperty("endpoints", out var endpointsProp)
        //     && endpointsProp.ValueKind is JsonValueKind.Array)
        // {
        //     var builder = ImmutableArray.CreateBuilder<OpenApiEndpointKey>();
        //     foreach (var item in endpointsProp.EnumerateArray())
        //     {
        //         if (item.ValueKind is JsonValueKind.Object
        //             && item.TryGetProperty("httpMethod", out var httpMethodProp)
        //             && httpMethodProp.ValueKind is JsonValueKind.String
        //             && item.TryGetProperty("route", out var routeProp)
        //             && routeProp.ValueKind is JsonValueKind.String)
        //         {
        //             builder.Add(new OpenApiEndpointKey(httpMethodProp.GetString()!, routeProp.GetString()!));
        //         }
        //     }
        //     endpoints = builder.ToImmutable();
        // }
        //
        // var models = ImmutableArray<string>.Empty;
        // if (root.TryGetProperty("models", out var modelsProp)
        //     && modelsProp.ValueKind is JsonValueKind.Array)
        // {
        //     var builder = ImmutableArray.CreateBuilder<string>();
        //     foreach (var item in modelsProp.EnumerateArray())
        //     {
        //         if (item.ValueKind is JsonValueKind.String)
        //         {
        //             builder.Add(item.GetString()!);
        //         }
        //     }
        //     models = builder.ToImmutable();
        // }
        //
        // return new ArchiveMetadata
        // {
        //     FormatVersion = new Version(formatVersionProp.GetString()!),
        //     Endpoints = endpoints,
        //     Models = models
        // };
    }
}
