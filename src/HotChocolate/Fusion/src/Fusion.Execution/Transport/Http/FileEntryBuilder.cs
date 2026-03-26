using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using HotChocolate.Text.Json;
using HotChocolate.Types;
using HotChocolate.Buffers;

namespace HotChocolate.Fusion.Transport.Http;

/// <summary>
/// Prepares variable JSON for the GraphQL multipart request specification by
/// extracting file references and producing the cleaned operations JSON with
/// <c>null</c> placeholders and the file map needed for multipart form construction.
/// </summary>
internal static class FileEntryBuilder
{
    private static ReadOnlySpan<byte> FileMarkerPrefix => "$.file("u8;

    public static (JsonSegment CleanedJson, ImmutableArray<FileEntry> FileMap) Build(
        ChunkedArrayWriter writer,
        JsonSegment variables,
        IFileLookup fileLookup,
        string pathPrefix = "variables")
    {
        var fileEntries = ImmutableArray.CreateBuilder<FileEntry>();
        var cleanedJson = Build(writer, variables, fileLookup, fileEntries, pathPrefix);
        return (cleanedJson, fileEntries.ToImmutable());
    }

    public static JsonSegment Build(
        ChunkedArrayWriter writer,
        JsonSegment variables,
        IFileLookup fileLookup,
        ImmutableArray<FileEntry>.Builder fileEntries,
        string pathPrefix = "variables")
    {
        var sequence = variables.AsSequence();
        var reader = new Utf8JsonReader(sequence, isFinalBlock: true, default);
        var startPosition = writer.Position;
        var jsonWriter = new JsonWriter(writer, new JsonWriterOptions { Indented = false });
        var hasFiles = false;
        var path = ParsePrefix(pathPrefix);

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    jsonWriter.WriteStartObject();
                    break;

                case JsonTokenType.EndObject:
                    jsonWriter.WriteEndObject();
                    path = PopAfterValue(path);
                    break;

                case JsonTokenType.StartArray:
                    jsonWriter.WriteStartArray();
                    path = path.Append(0);
                    break;

                case JsonTokenType.EndArray:
                    jsonWriter.WriteEndArray();
                    // Pop the array index, then pop after value (property name or parent array index).
                    path = path.Parent;
                    path = PopAfterValue(path);
                    break;

                case JsonTokenType.PropertyName:
                    jsonWriter.WritePropertyName(reader.ValueSpan);
                    path = path.Append(reader.GetString()!);
                    break;

                case JsonTokenType.String:
                    if (TryExtractFileKey(reader.ValueSpan, out var fileKey)
                        && fileLookup.TryGetFile(fileKey, out var file))
                    {
                        fileEntries.Add(new FileEntry(fileKey, PrintPath(path), file));
                        jsonWriter.WriteNullValue();
                        hasFiles = true;
                    }
                    else
                    {
                        jsonWriter.WriteStringValue(reader.ValueSpan);
                    }
                    path = PopAfterValue(path);
                    break;

                case JsonTokenType.Number:
                    jsonWriter.WriteRawValue(reader.ValueSpan);
                    path = PopAfterValue(path);
                    break;

                case JsonTokenType.True:
                    jsonWriter.WriteBooleanValue(true);
                    path = PopAfterValue(path);
                    break;

                case JsonTokenType.False:
                    jsonWriter.WriteBooleanValue(false);
                    path = PopAfterValue(path);
                    break;

                case JsonTokenType.Null:
                    jsonWriter.WriteNullValue();
                    path = PopAfterValue(path);
                    break;
            }
        }

        if (!hasFiles)
        {
            writer.ResetTo(startPosition);
            return variables;
        }

        return JsonSegment.Create(writer, startPosition, writer.Position - startPosition);
    }

    private static Path ParsePrefix(string pathPrefix)
    {
        var path = Path.Root;

        foreach (var segment in pathPrefix.Split('.'))
        {
            path = int.TryParse(segment, out var index)
                ? path.Append(index)
                : path.Append(segment);
        }

        return path;
    }

    /// <summary>
    /// After a value is consumed, pops the current path segment:
    /// for object properties, removes the property name;
    /// for array elements, advances to the next index.
    /// </summary>
    private static Path PopAfterValue(Path path)
    {
        if (path.Parent is not null && path is IndexerPathSegment indexer)
        {
            // Inside an array — advance to next index.
            return path.Parent.Append(indexer.Index + 1);
        }

        if (path.Parent is not null && path is NamePathSegment)
        {
            // Inside an object — pop the property name.
            return path.Parent;
        }

        return path;
    }

    /// <summary>
    /// Prints the path using dot notation (e.g., "variables.input.files.0")
    /// as required by the GraphQL multipart request specification.
    /// </summary>
    private static string PrintPath(Path path)
    {
        var segments = path.ToList();

        if (segments.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        for (var i = 0; i < segments.Count; i++)
        {
            if (i > 0)
            {
                sb.Append('.');
            }

            switch (segments[i])
            {
                case string name:
                    sb.Append(name);
                    break;

                case int index:
                    sb.Append(index);
                    break;
            }
        }

        return sb.ToString();
    }

    private static bool TryExtractFileKey(ReadOnlySpan<byte> value, out string key)
    {
        if (value.Length > FileMarkerPrefix.Length + 1
            && value.StartsWith(FileMarkerPrefix)
            && value[^1] == (byte)')')
        {
            key = Encoding.UTF8.GetString(
                value.Slice(FileMarkerPrefix.Length, value.Length - FileMarkerPrefix.Length - 1));
            return key.Length > 0;
        }

        key = default!;
        return false;
    }
}
