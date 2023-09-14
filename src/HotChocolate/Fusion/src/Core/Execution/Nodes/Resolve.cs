using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Fusion.Clients;
using static HotChocolate.Fusion.Execution.ExecutorUtils;
using static HotChocolate.Fusion.Execution.Nodes.ResolverNodeBase;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// The resolver node is responsible for fetching data from a subgraph.
/// </summary>
/// <param name="id">
/// The unique id of this node.
/// </param>
/// <param name="config">
/// Gets the resolver configuration.
/// </param>
internal sealed class Resolve(int id, Config config) : ResolverNodeBase(id, config)
{

    /// <summary>
    /// Gets the kind of this node.
    /// </summary>
    public override QueryPlanNodeKind Kind => QueryPlanNodeKind.Resolve;

    /// <summary>
    /// Executes this resolver node.
    /// </summary>
    /// <param name="context">
    /// The execution context.
    /// </param>
    /// <param name="state">
    /// The execution state.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    protected override async Task OnExecuteAsync(
        FusionExecutionContext context,
        RequestState state,
        CancellationToken cancellationToken)
    {
        if (!state.TryGetState(SelectionSet, out var executionState))
        {
            return;
        }

        try
        {
            var requests = new SubgraphGraphQLRequest[executionState.Count];

            // first we will create request for all of our selection sets.
            InitializeRequests(context, executionState, requests);

            // once we have the requests, we will enqueue them for execution with the execution engine.
            // the execution engine will batch these requests if possible.
            var responses = await context.ExecuteAsync(SubgraphName, requests, cancellationToken).ConfigureAwait(false);

            // before we extract the data from the responses we will enqueue the responses
            // for cleanup so that the memory can be released at the end of the execution.
            // Since we are not fully deserializing the responses we cannot release the memory here
            // but need to wait until the transport layer is finished and disposes the result.
            context.Result.RegisterForCleanup(responses, ReturnResults);

            // we need to lock the state before mutating it since there could be multiple
            // query plan nodes be interested in it.
            lock (executionState)
            {
                ProcessResponses(context, executionState, requests, responses, SubgraphName);
            }
        }
        catch(Exception ex)
        {
            var error = context.OperationContext.ErrorHandler.CreateUnexpectedError(ex);
            context.Result.AddError(error.Build());
        }
    }

    /// <summary>
    /// Executes this resolver node.
    /// </summary>
    /// <param name="context">
    /// The execution context.
    /// </param>
    /// <param name="state">
    /// The execution state.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    protected override async Task OnExecuteNodesAsync(
        FusionExecutionContext context,
        RequestState state,
        CancellationToken cancellationToken)
    {
        if (state.ContainsState(SelectionSet))
        {
            await base.OnExecuteNodesAsync(context, state, cancellationToken).ConfigureAwait(false);
        }
    }

    private void InitializeRequests(
        FusionExecutionContext context,
        List<ExecutionState> executionState,
        SubgraphGraphQLRequest[] requests)
    {
        ref var state = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(executionState));
        ref var request = ref MemoryMarshal.GetArrayDataReference(requests);
        ref var end = ref Unsafe.Add(ref state, executionState.Count);

        while (Unsafe.IsAddressLessThan(ref state, ref end))
        {
            TryInitializeExecutionState(context.QueryPlan, state);
            request = CreateRequest(context.OperationContext.Variables, state.VariableValues);

            state = ref Unsafe.Add(ref state, 1)!;
            request = ref Unsafe.Add(ref request, 1)!;
        }
    }

    private void ProcessResponses(
        FusionExecutionContext context,
        List<ExecutionState> executionStates,
        SubgraphGraphQLRequest[] requests,
        GraphQLResponse[] responses,
        string subgraphName)
    {
        ref var state = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(executionStates));
        ref var request = ref MemoryMarshal.GetArrayDataReference(requests);
        ref var response = ref MemoryMarshal.GetArrayDataReference(responses);
        ref var end = ref Unsafe.Add(ref state, executionStates.Count);

        while (Unsafe.IsAddressLessThan(ref state, ref end))
        {
            var data = UnwrapResult(response);
            var selectionSet = state.SelectionSet;
            var selectionResults = state.SelectionSetData;
            var exportKeys = state.Requires;
            var variableValues = state.VariableValues;

            ExtractErrors(context.Result, response.Errors, context.ShowDebugInfo);

            // we extract the selection data from the request and add it to the
            // workItem results.
            ExtractSelectionResults(SelectionSet, subgraphName, data, selectionResults);

            // next we need to extract any variables that we need for followup requests.
            ExtractVariables(data, context.QueryPlan, selectionSet, exportKeys, variableValues);

            state = ref Unsafe.Add(ref state, 1)!;
            request = ref Unsafe.Add(ref request, 1)!;
            response = ref Unsafe.Add(ref response, 1)!;
        }
    }
}
