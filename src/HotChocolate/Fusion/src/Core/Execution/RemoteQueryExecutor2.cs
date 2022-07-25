using System.Text.Json;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Planning;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

internal sealed class RemoteQueryExecutor2
{
    private readonly object _sync = new();
    private Metadata.Schema _schema;
    private readonly RemoteRequestExecutorFactory _executorFactory;
    private readonly List<ExecutionNode> _completed = new();
    private readonly List<Response> _responses = new();
    private int _completedCount;

    public RemoteQueryExecutor2(RemoteRequestExecutorFactory executorFactory)
    {
        _executorFactory = executorFactory;
    }

    public async Task ExecuteAsync(OperationContext context, QueryPlan plan, IExecutionState state)
    {
        var tasks = new Dictionary<RequestNode, Task<Response>>();
        var dict = new Dictionary<ISelection, RequestNode>();

        foreach (var requestNode in plan.GetRequestNodes(context.Operation.RootSelectionSet))
        {
            tasks.Add(ExecuteNode(requestNode, state, context.RequestAborted));
        }


        var next = new Stack<(ISelectionSet SelectionSet, object Response, ObjectResult Result)>();
        var ready = new Stack<(ISelectionSet SelectionSet, object Response, ObjectResult Result)>();

        do
        {
            while (ready.TryPop(out var backlogItem))
            {
                var selections = backlogItem.SelectionSet.Selections;
                var response = (JsonElement[])backlogItem.Response;
                var result = backlogItem.Result;

                for (var i = 0; i < backlogItem.SelectionSet.Selections.Count; i++)
                {
                    var selection = selections[i];
                    var element = response[i];
                    var isNullable = selection.TypeKind is not TypeKind.NonNull;

                    if (element.ValueKind is JsonValueKind.Null ||
                        selection.Type.NamedType().IsLeafType())
                    {
                        result.SetValueUnsafe(i, selection.ResponseName, element, isNullable);
                    }
                    else if (selection.Type.IsListType())
                    {
                        var elementType = selection.Type.ElementType();
                        var list = context.Result.RentList(element.GetArrayLength());

                        foreach (var item in element.EnumerateArray())
                        {
                            var itemResult = context.Result.RentObject(4);


                        }
                    }
                    else
                    {
                        var operation = context.Operation;

                        // this is the type name in that schema ... so we need a reverse lookup here
                        var typeName = element.GetProperty("__typename").GetString()!;


                    }
                }
            }

            // batch

        } while (ready.Count > 0);


        foreach (var selection in context.Operation.RootSelectionSet.Selections)
        {



        }




    }

    private Task<object> ComposeResult(ISelectionSet selectionSet, )
    {

    }

    private async Task<Response> ExecuteNode(
        RequestNode requestNode,
        IExecutionState state,
        CancellationToken ct)
    {
            var request = requestNode.Handler.CreateRequest(state);
            var executor = _executorFactory.Create(request.SchemaName);
            var response = await executor.ExecuteAsync(request, ct);

            BuildResult(response, null);

            lock (_sync)
            {
                _completed.Add(requestNode);
            }

    }

    private void BuildResult(Response response, object responseNode)
    {

    }
}
