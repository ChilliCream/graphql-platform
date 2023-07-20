using System.Buffers;
using System.Runtime.CompilerServices;
using HotChocolate.Execution;
using HotChocolate.Execution.DependencyInjection;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Serialization;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Fusion.Execution.ExecutorUtils;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// A subscribe represents a subscription operation that is executed on a subgraph.
/// </summary>
internal sealed class Subscribe : ResolverNodeBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="Subscribe"/>.
    /// </summary>
    /// <param name="id">
    /// The unique id of this node.
    /// </param>
    /// <param name="subgraphName">
    /// The name of the subgraph on which this request handler executes.
    /// </param>
    /// <param name="document">
    /// The GraphQL request document.
    /// </param>
    /// <param name="selectionSet">
    /// The selection set for which this request provides a patch.
    /// </param>
    /// <param name="requires">
    /// The variables that this request handler requires to create a request.
    /// </param>
    /// <param name="path">
    /// The path to the data that this request handler needs to extract.
    /// </param>
    /// <param name="forwardedVariables">
    /// The variables that this request handler forwards to the subgraph.
    /// </param>
    /// <param name="transportFeatures">
    /// The transport features that are required by this node.
    /// </param>
    public Subscribe(
        int id,
        string subgraphName,
        DocumentNode document,
        ISelectionSet selectionSet,
        IReadOnlyList<string> requires,
        IReadOnlyList<string> path,
        IReadOnlyList<string> forwardedVariables,
        TransportFeatures transportFeatures)
        : base(id, subgraphName, document, selectionSet, requires, path, forwardedVariables, transportFeatures)
    {
    }

    /// <summary>
    /// Gets the kind of this node.
    /// </summary>
    public override QueryPlanNodeKind Kind => QueryPlanNodeKind.Subscribe;

    /// <summary>
    /// Subscribes to a subgraph subscription and runs the query plan every
    /// time the subscription yields a new response.
    /// </summary>
    /// <param name="rootContext">
    /// The root execution context.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// The query result stream.
    /// </returns>
    internal async IAsyncEnumerable<IQueryResult> SubscribeAsync(
        FusionExecutionContext rootContext,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var variableValues = new Dictionary<string, IValueNode>();
        var request = CreateRequest(rootContext.OperationContext.Variables, variableValues);
        var initialResponse = true;

        await foreach (var response in rootContext
            .SubscribeAsync(SubgraphName, request, cancellationToken)
            .ConfigureAwait(false))
        {
            // we clone a operation context for each response so that we have a clean slate for
            // every execution and no state leaks into the next execution.
            var context = rootContext.Clone();
            var operationContext = context.OperationContext;

                // we ensure that the query plan is only included once per stream
            // in order to not inflate response sizes.
            if (initialResponse)
            {
                // if we find a request context data key `IncludeQueryPlan` that indicates
                // that the query plan shall be written into the response we will do so.
                if (operationContext.ContextData.ContainsKey(IncludeQueryPlan))
                {
                    var bufferWriter = new ArrayBufferWriter<byte>();
                    context.QueryPlan.Format(bufferWriter);
                    operationContext.Result.SetExtension(
                        FusionContextDataKeys.QueryPlan,
                        new RawJsonValue(bufferWriter.WrittenMemory));
                }

                // We store the query plan on the result for unit tests and other inspection.
                operationContext.Result.SetContextData(
                    FusionContextDataKeys.QueryPlan,
                    context.QueryPlan);
            }
            initialResponse = false;

            // Before we can start building requests we need to rent state for the execution result.
            var rootSelectionSet = context.Operation.RootSelectionSet;
            var rootResult = context.Result.RentObject(rootSelectionSet.Selections.Count);

            // by registering the state we will get a work item which represents the state for
            // this execution step.
            var workItem = context.RegisterState(rootSelectionSet, rootResult);

            // Since we are at the root level we need to register the result object we rented
            // as data property of the GraphQL request.
            context.Result.SetData(rootResult);

            // In a normal execution plan node we would be ready to execute now.
            // In this case the result was pushed to us by the subgraph subscription.
            // So we skip execution and just unwrap the result and extract the selection data.
            var data = UnwrapResult(response);
            var selectionResults = workItem.SelectionSetData;
            var exportKeys = workItem.ExportKeys;
            variableValues = workItem.VariableValues;

            // we extract the selection data from the request and add it to the workItem results.
            ExtractSelectionResults(SelectionSet, SubgraphName, data, selectionResults);

            // Next we need to extract any variables that we need for followup requests.
            // The variables we extract here are requirements for the next execution step.
            ExtractVariables(data, context.QueryPlan, SelectionSet, exportKeys, variableValues);

            // We now execute the rest of the execution tree.
            await ExecuteAsync(context, cancellationToken).ConfigureAwait(false);

            // Before we yield back the result we register with it the rented operation context.
            // When the result is disposed in the transport after usage
            // so will the operation context.
            context.Result.RegisterForCleanup(
                () =>
                {
                    context.Dispose();
                    return default;
                });

            yield return context.Result.BuildResult();
        }
    }
}

static file class SubscriptionNodeExtensions
{
    public static FusionExecutionContext Clone(this FusionExecutionContext context)
    {
        var owner = context.CreateOperationContextOwner();
        return FusionExecutionContext.CreateFrom(context, owner);
    }

    private static OperationContextOwner CreateOperationContextOwner(
        this FusionExecutionContext context)
    {
        var owner = context.OperationContext.Services
            .GetRequiredService<IFactory<OperationContextOwner>>()
            .Create();
        owner.OperationContext.InitializeFrom(context.OperationContext);
        return owner;
    }
}
