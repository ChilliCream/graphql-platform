using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Execution.DependencyInjection;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Serialization;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using GraphQLRequest = HotChocolate.Fusion.Clients.GraphQLRequest;

namespace HotChocolate.Fusion.Planning;

internal sealed class SubscriptionNode : QueryPlanNode
{
    private readonly IReadOnlyList<string> _path;

    public SubscriptionNode(
        int id,
        string subgraphName,
        DocumentNode document,
        ISelectionSet selectionSet,
        IReadOnlyList<string> requires,
        IReadOnlyList<string> path)
        : base(id)
    {
        _path = path;
        SubgraphName = subgraphName;
        Document = document;
        SelectionSet = selectionSet;
        Requires = requires;
    }

    public override QueryPlanNodeKind Kind => QueryPlanNodeKind.Subscription;

    /// <summary>
    /// Gets the schema name on which this request handler executes.
    /// </summary>
    public string SubgraphName { get; }

    /// <summary>
    /// Gets the GraphQL request document.
    /// </summary>
    public DocumentNode Document { get; }

    /// <summary>
    /// Gets the selection set for which this request provides a patch.
    /// </summary>
    public ISelectionSet SelectionSet { get; }

    /// <summary>
    /// Gets the variables that this request handler requires to create a request.
    /// </summary>
    public IReadOnlyList<string> Requires { get; }

    internal async IAsyncEnumerable<IQueryResult> SubscribeAsync(
        FusionExecutionContext rootContext,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var variableValues = new Dictionary<string, IValueNode>();
        var request = CreateRequest(variableValues);
        var initialResponse = true;

        await foreach (var response in rootContext
            .SubscribeAsync(SubgraphName, request, cancellationToken)
            .ConfigureAwait(false))
        {
            var context = rootContext.Clone();
            var operationContext = context.OperationContext;

            if (initialResponse)
            {
                if (operationContext.ContextData.ContainsKey(WellKnownContextData.IncludeQueryPlan))
                {
                    var bufferWriter = new ArrayBufferWriter<byte>();
                    context.QueryPlan.Format(bufferWriter);
                    operationContext.Result.SetExtension(
                        "queryPlan",
                        new RawJsonValue(bufferWriter.WrittenMemory));
                }

                // we store the context on the result for unit tests.
                operationContext.Result.SetContextData("queryPlan", context.QueryPlan);
            }
            initialResponse = false;

            // Enqueue root node to initiate the execution process.
            var rootSelectionSet = context.Operation.RootSelectionSet;
            var rootResult = context.Result.RentObject(rootSelectionSet.Selections.Count);

            var workItem = context.RegisterState(rootSelectionSet, rootResult);
            context.Result.SetData(rootResult);

            var data = UnwrapResult(response);
            var selections = workItem.SelectionSet.Selections;
            var selectionResults = workItem.SelectionResults;
            var exportKeys = workItem.ExportKeys;
            variableValues = workItem.VariableValues;

            // we extract the selection data from the request and add it to the workItem results.
            ExecutorUtils.ExtractSelectionResults(selections, SubgraphName, data, selectionResults);

            // next we need to extract any variables that we need for followup requests.
            ExecutorUtils.ExtractVariables(data, exportKeys, variableValues);

            await ExecuteAsync(context, cancellationToken).ConfigureAwait(false);

            context.Result.RegisterForCleanup(
                () =>
                {
                    context.Dispose();
                    return default;
                });

            yield return context.Result.BuildResult();
        }
    }

    private GraphQLRequest CreateRequest(IReadOnlyDictionary<string, IValueNode> variableValues)
    {
        ObjectValueNode? vars = null;

        if (Requires.Count > 0)
        {
            var fields = new List<ObjectFieldNode>();

            foreach (var requirement in Requires)
            {
                if (variableValues.TryGetValue(requirement, out var value))
                {
                    fields.Add(new ObjectFieldNode(requirement, value));
                }
                else
                {
                    // TODO : error helper
                    throw new ArgumentException(
                        $"The variable value `{requirement}` was not provided " +
                        "but is required.",
                        nameof(variableValues));
                }
            }

            vars ??= new ObjectValueNode(fields);
        }

        return new GraphQLRequest(SubgraphName, Document, vars, null);
    }

    private JsonElement UnwrapResult(GraphQLResponse response)
    {
        if (_path.Count == 0)
        {
            return response.Data;
        }

        if (response.Data.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null)
        {
            var current = response.Data;

            for (var i = 0; i < _path.Count; i++)
            {
                if (current.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                {
                    return current;
                }

                current.TryGetProperty(_path[i], out var propertyValue);
                current = propertyValue;
            }

            return current;
        }

        return response.Data;
    }
}

static file class SubscriptionNodeExtensions
{
    private static OperationContextOwner CreateOperationContextOwner(
        this FusionExecutionContext context)
    {
        var owner = context.OperationContext.Services
            .GetRequiredService<IFactory<OperationContextOwner>>()
            .Create();
        owner.OperationContext.InitializeFrom(context.OperationContext);
        return owner;
    }

    public static FusionExecutionContext Clone(this FusionExecutionContext context)
    {
        var owner = context.CreateOperationContextOwner();
        return FusionExecutionContext.CreateFrom(context, owner);
    }
}
