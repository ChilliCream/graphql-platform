using System.Collections.Immutable;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Execution;
using static HotChocolate.Fusion.Execution.ExecutorUtils;

namespace HotChocolate.Fusion.Planning;

internal sealed class FetchNode : QueryPlanNode
{
    public FetchNode(RequestHandler handler)
    {
        Handler = handler;
    }

    public RequestHandler Handler { get; }

    public override bool AppliesTo(ISelectionSet selectionSet)
        => Handler.SelectionSet.Equals(selectionSet);

    internal override async Task ExecuteAsync(
        IFederationContext context,
        IReadOnlyList<WorkItem> workItems,
        CancellationToken cancellationToken)
    {
        var schemaName = Handler.SchemaName;
        var requests = new GraphQLRequest[workItems.Count];

        // first we will create a request for all of our work items.
        for (var i = 0; i < workItems.Count; i++)
        {
            requests[i] = Handler.CreateRequest(workItems[i].VariableValues);
        }

        // once we have the requests, we will enqueue them for execution with the execution engine.
        // the execution engine will batch these requests if possible.
        var responses = await context.ExecuteAsync(
            schemaName,
            requests,
            cancellationToken)
            .ConfigureAwait(false);

        // before we extract the data from the responses we will enqueue the responses for cleanup
        // so that the memory can be released at the end of the execution.
        // Since we are not fully deserializing the responses we cannot release the memory here
        // but need to wait until the transport layer is finished and disposes the result.
        context.Result.RegisterForCleanup(
            responses,
            r =>
            {
                for (var i = 0; i < r.Count; i++)
                {
                    r[i].Dispose();
                }
                return default!;
            });

        // At this section we are extracting the result from the response data.
        var selections = workItems[0].SelectionSet.Selections;

        for(var i = 0; i < requests.Length; i++)
        {
            var response = responses[i];
            var data = Handler.UnwrapResult(response);
            var workItem = workItems[i];
            var selectionResults = workItem.SelectionResults;
            var exportKeys = workItem.ExportKeys;
            var variableValues = workItem.VariableValues;

            // we extract the selection data from the request and add it to the workItem results.
            ExtractSelectionResults(selections, schemaName, data, selectionResults);

            // next we need to extract any variables that we need for followup requests.
            ExtractVariables(data, exportKeys, variableValues);
        }
    }
}

internal sealed class ComposeNode : QueryPlanNode
{
    private readonly IReadOnlyList<ISelectionSet> _selectionSet;

    public ComposeNode(IReadOnlyList<ISelectionSet> selectionSet)
    {
        _selectionSet = selectionSet ?? throw new ArgumentNullException(nameof(selectionSet));
    }

    public override bool AppliesTo(ISelectionSet selectionSet)
    {
        for (var i = 0; i < _selectionSet.Count; i++)
        {
            if (_selectionSet[i].Equals(selectionSet))
            {
                return true;
            }
        }

        return false;
    }

    internal  override Task ExecuteAsync(
        IFederationContext context,
        IReadOnlyList<WorkItem> workItems,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < workItems.Count; i++)
        {
            ComposeResult(context, workItems[i]);
        }

        return Task.CompletedTask;
    }
}

internal sealed class IntrospectionNode : QueryPlanNode
{
    private readonly ISelectionSet _selectionSet;

    public IntrospectionNode(ISelectionSet selectionSet)
    {
        _selectionSet = selectionSet ?? throw new ArgumentNullException(nameof(selectionSet));
    }

    public override bool AppliesTo(ISelectionSet selectionSet)
        => _selectionSet.Equals(selectionSet);

    internal override async Task ExecuteAsync(
        IFederationContext context,
        IReadOnlyList<WorkItem> workItems,
        CancellationToken cancellationToken)
    {
        var operationContext = context.OperationContext;
        var rootSelections = _selectionSet.Selections;
        var workItem = workItems.Single();

        for (var i = 0; i < rootSelections.Count; i++)
        {
            var selection = rootSelections[i];
            if (selection.Field.IsIntrospectionField)
            {
                var resolverTask = operationContext.CreateResolverTask(
                    selection,
                    operationContext.RootValue,
                    workItem.Result,
                    i,
                    operationContext.PathFactory.Append(Path.Root, selection.ResponseName),
                    ImmutableDictionary<string, object?>.Empty);
                resolverTask.BeginExecute(cancellationToken);

                await resolverTask.WaitForCompletionAsync(cancellationToken);
            }
        }
    }
}
