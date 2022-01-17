using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Processing.Plan;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Stitching.Execution;

public interface IFetcher
{
    ValueTask<IFetchResponse> FetchAsync(FetchRequest request);
}

public sealed class FetchRequest
{
    public FetchRequest(
        NameString source,
        DocumentNode document,
        IReadOnlyDictionary<string, object?>? variables = null,
        IReadOnlyDictionary<string, object?>? extensions = null,
        IReadOnlyDictionary<string, object?>? contextData = null)
    {
        Source = source.EnsureNotEmpty(nameof(source));
        Document = document ?? throw new ArgumentNullException(nameof(document));
        Variables = variables;
        Extensions = extensions;
        ContextData = contextData;
    }

    public NameString Source { get; }

    public DocumentNode Document { get; }

    public IReadOnlyDictionary<string, object?>? Variables { get; }

    public IReadOnlyDictionary<string, object?>? Extensions { get; }

    public IReadOnlyDictionary<string, object?>? ContextData { get; }
}


/// <summary>
/// Represents result from a GraphQL service fetcher.
/// </summary>
public interface IFetchResponse : IExecutionResult
{
    /// <summary>
    /// A string that was passed to the label argument of the @defer or @stream 
    /// directive that corresponds to this results.
    /// </summary>
    /// <value></value>
    string? Label { get; }

    /// <summary>
    ///  A path to the insertion point that informs the client how to patch a 
    /// subsequent delta payload into the original payload.
    /// </summary>
    /// <value></value>
    Path? Path { get; }

    /// <summary>
    /// The data that is being delivered.
    /// </summary>
    object? Data { get; }

    /// <summary>
    /// A boolean that is present and <c>true</c> when there are more payloads 
    /// that will be sent for this operation. The last payload in a multi payload response 
    /// should return HasNext: <c>false</c>. 
    /// HasNext is null for single-payload responses to preserve backwards compatibility.
    /// </summary>
    bool? HasNext { get; }
}

internal static class Resolvers
{
    public static ValueTask<object> ResolveAsync(IPureResolverContext context)
    {
        var parent = context.Parent<ObjectValueNode>();
        var selection = (ISelection)context.Selection;

        // return parent.Fields[0].
        return default;
    }
}

internal static class ResultMergeHelper
{
    public static void MergeObjects(List<object?> objects)
    {
        var first = objects[0];

        for (int i = 1; i < objects.Count; i++)
        {
            Merge(first, objects[i]);
        }
    }

    public static void Merge(object? a, object? b)
    {
        if (a is Dictionary<string, object?> dictA && b is Dictionary<string, object?> dictB)
        {
            MergeObjects(dictA, dictB);
        }
        else if (a is List<object?> listA && b is List<object?> listB)
        {
            MergeLists(listA, listB);
        }
    }

    private static void MergeObjects(Dictionary<string, object?> a, Dictionary<string, object?> b)
    {
        foreach (var item in b)
        {
            a.TryAdd(item.Key, item.Value);
        }
    }

    private static void MergeLists(List<object?> a, List<object?> b)
    {
        if (a.Count > 0 && a.Count == b.Count)
        {
            for (int i = 0; i < a.Count; i++)
            {
                Merge(a[i], b[i]);
            }
        }
    }
}

internal sealed class FetchExecutionStep : ExecutionStep
{
    public override bool TryActivate(IQueryPlanState state)
    {
        GetFetchState(state);
        return true;
    }

    public override void CompleteTask(IQueryPlanState state, IExecutionTask task)
    {
        
    }

    private FetchState GetFetchState(IQueryPlanState state)
    {
        if (!state.Context.ContextData.TryGetValue(nameof(FetchState), out var value) ||
            value is not FetchState fetchState)
        {
            fetchState = new FetchState();
            state.Subscribe(fetchState);
            state.Context.ContextData.Add(nameof(FetchState), fetchState);
        }

        return fetchState;
    }
}

internal sealed class FetchState : IObserver<ResolverResult>
{
    public void OnNext(ResolverResult value)
    {

    }

    public void OnError(Exception error)
    {
    }

    public void OnCompleted()
    {
    }

    private static Path ToHierarchy(Path path)
    {
        Path? current = path;
        string[] rented = ArrayPool<string>.Shared.Rent(path.Depth);
        int i = 0;

        while (current is not null && current is not RootPathSegment)
        {
            if (current is NamePathSegment namePath)
            {
                rented[i++] = namePath.Name.Value;
            }

            current = current.Parent;
        }

        current = Path.Root;

        for (int j = i - 1; j >= 0; j--)
        {
            current = current.Append(rented[j]);
        }

        ArrayPool<string>.Shared.Return(rented);
        return current;
    }
}

internal sealed class FetchTask : ExecutionTask
{
    private readonly IFetcher _fetcher;
    private readonly IReadOnlyDictionary<string, object?> _variables;
    private readonly QueryNode _root;


    public FetchTask(IExecutionTaskContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    protected override IExecutionTaskContext Context { get; }

    protected override async ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        var variables = new Dictionary<string, object?>();
        var queue = new Queue<QueryNode>();
        queue.Enqueue(_root);

        while (queue.Count > 0)
        {
            QueryNode current = queue.Dequeue();
            FetchRequest request = new(current.Source, current.Document!, variables);
            IFetchResponse response = await _fetcher.FetchAsync(request).ConfigureAwait(false);



        }
    }


}

internal sealed class FetchTaskDefinition : IExecutionTaskDefinition
{
    public IExecutionTask Create(IExecutionTaskContext context)
    {
        throw new NotImplementedException();
    }
}