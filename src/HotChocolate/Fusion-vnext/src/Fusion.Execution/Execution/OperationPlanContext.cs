using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

public class OperationPlanContext
{
    public required OperationPlan OperationPlan { get; init; }

    public required GraphQLRequestContext RequestContext { get; init; }

    public FetchResultStore ResultStore { get; } = new();

    public ImmutableArray<VariableValues> CreateVariables(ImmutableArray<OperationRequirement> requirements)
    {
        throw new NotImplementedException();
    }

    public IGraphQLClient GetClient(string schemaName)
    {
        throw new NotImplementedException();
    }
}

public sealed class FetchResultStore
{
    private readonly ConcurrentDictionary<Path, List<FetchResult>> _results = new();

    public void Add(FetchResult result)
    {
        var results = _results.GetOrAdd(result.Path, _ => []);

        lock (results)
        {
            results.Add(result);
        }
    }
}

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

    public JsonElement Data { get; init; }

    public JsonElement Errors { get; init; }

    public JsonElement Extensions { get; init; }

    public JsonElement GetFromSourceData()
        => _source ??= GetFromData(Source);

    public JsonElement GetFromData(SelectionPath path)
    {
        if (path == SelectionPath.Root)
        {
            return Data;
        }

        var current = Data;

        foreach (var segment in path.Segments)
        {
            if (current.ValueKind != JsonValueKind.Object)
            {
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

    public static FetchResult From(OperationExecutionNode node, GraphQLResult result)
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
}

public sealed class VariableValues
{
    public required Path Path { get; init; }

    public required ObjectValueNode Values { get; init; }
}
