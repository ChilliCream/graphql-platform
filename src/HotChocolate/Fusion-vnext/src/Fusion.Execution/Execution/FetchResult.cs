using System.Text.Json;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Execution;

public sealed class FetchResult : IComparable<FetchResult>, IDisposable
{
    private readonly IDisposable _resource;
    private JsonElement? _source;

    private FetchResult(IDisposable resource)
    {
        _resource = resource;
    }

    /// <summary>
    /// Gets the runtime path.
    /// </summary>
    public Path Path { get; init; } = Path.Root;

    /// <summary>
    /// Gets the path to the selection set for which this data was fetched.
    /// </summary>
    public SelectionPath Target { get; init; } = SelectionPath.Root;

    /// <summary>
    /// Gets the path to the local selection set (the selection set within the source schema request)
    /// to extract the data from.
    /// </summary>
    public SelectionPath Source { get; init; } = SelectionPath.Root;

    /// <summary>
    /// Gets the execution node id that was used to fetch the result.
    /// </summary>
    public int ExecutionNodeId { get; init; }

    public JsonElement Data { get; init; }

    public JsonElement Errors { get; init; }

    public JsonElement Extensions { get; init; }

    public JsonElement GetFromSourceData()
        => _source ??= GetFromData(Data, Source);

    private static JsonElement GetFromData(JsonElement element, SelectionPath path)
    {
        if (path == SelectionPath.Root)
        {
            return element;
        }

        var current = element;

        foreach (var segment in path.Segments)
        {
            if (current.ValueKind == JsonValueKind.Null)
            {
                return current;
            }

            if (current.ValueKind != JsonValueKind.Object)
            {
                return default;
            }

            if (segment.Kind == SelectionPathSegmentKind.InlineFragment)
            {
                if (current.TryGetProperty("__typename", out var typeProperty)
                    && typeProperty.ValueKind == JsonValueKind.String
                    && typeProperty.ValueEquals(segment.Name))
                {
                    continue;
                }

                return default;
            }

            if (current.TryGetProperty(segment.Name, out var property))
            {
                current = property;
            }
            else
            {
                return default;
            }
        }

        return current;
    }

    public int CompareTo(FetchResult? other)
    {
        if (other is null)
        {
            return -1;
        }

        return Path.CompareTo(other.Path);
    }

    public void Dispose() => _resource.Dispose();

    public static FetchResult From(OperationExecutionNode node, SourceSchemaResult result)
    {
        return new FetchResult(result)
        {
            Path = result.Path,
            Target = node.Target,
            Source = node.Source,
            Data = result.Data,
            Errors = result.Errors,
            Extensions = result.Extensions
        };
    }

    public static FetchResult From(Path path, SelectionPath target, SelectionPath source, JsonDocument result)
    {
        var root = result.RootElement;
        root.TryGetProperty("data", out var data);
        root.TryGetProperty("errors", out var errors);
        root.TryGetProperty("extensions", out var extensions);

        return new FetchResult(result)
        {
            Path = path,
            Target = target,
            Source = source,
            Data = data,
            Errors = errors,
            Extensions = extensions
        };
    }
}
