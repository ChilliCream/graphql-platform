using System.Text.Json;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Execution;
using HotChocolate.Language;
using static HotChocolate.Fusion.Execution.ExecutorUtils;
using GraphQLRequest = HotChocolate.Fusion.Clients.GraphQLRequest;

namespace HotChocolate.Fusion.Planning;

internal sealed class ResolverNode : ResolverNodeBase
{
    public ResolverNode(
        int id,
        string subgraphName,
        DocumentNode document,
        ISelectionSet selectionSet,
        IReadOnlyList<string> requires,
        IReadOnlyList<string> path,
        IReadOnlyList<string> forwardedVariables)
        : base(id, subgraphName, document, selectionSet, requires, path, forwardedVariables)
    {
    }

    public override QueryPlanNodeKind Kind => QueryPlanNodeKind.Resolver;

    protected override async Task OnExecuteAsync(
        FusionExecutionContext context,
        IExecutionState state,
        CancellationToken cancellationToken)
    {
        if (state.TryGetState(SelectionSet, out var workItems))
        {
            var schemaName = SubgraphName;
            var requests = new GraphQLRequest[workItems.Count];
            var selections = workItems[0].SelectionSet.Selections;

            // first we will create a request for all of our work items.
            for (var i = 0; i < workItems.Count; i++)
            {
                var workItem = workItems[i];
                ExtractPartialResult(workItem);
                requests[i] = CreateRequest(
                    context.OperationContext.Variables,
                    workItem.VariableValues);
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

            for (var i = 0; i < requests.Length; i++)
            {
                var response = responses[i];
                var data = UnwrapResult(response);
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

    protected override async Task OnExecuteNodesAsync(
        FusionExecutionContext context,
        IExecutionState state,
        CancellationToken cancellationToken)
    {
        if (state.ContainsState(SelectionSet))
        {
            await base.OnExecuteNodesAsync(context, state, cancellationToken).ConfigureAwait(false);
        }
    }
}
