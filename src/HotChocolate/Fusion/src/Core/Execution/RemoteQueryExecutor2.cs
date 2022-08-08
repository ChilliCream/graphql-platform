using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Planning;
using HotChocolate.Types;
using HotChocolate.Utilities;
using IType = HotChocolate.Fusion.Metadata.IType;
using ObjectType = HotChocolate.Fusion.Metadata.ObjectType;

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

    public Task ExecuteAsync(OperationContext context, QueryPlan plan, IExecutionState state, CancellationToken cancellationToken)
    {
        var rootSelectionSet = context.Operation.RootSelectionSet;
        var rootResult = context.Result.RentObject(rootSelectionSet.Selections.Count);
        var rootWorkItem = new WorkItem(rootSelectionSet, new ArgumentContext(), rootResult);
        var internalContext = new ExecutorContext();
        internalContext.Fetch.Add(rootWorkItem);
        return ExecuteAsync(internalContext, cancellationToken);
    }

    private async Task ExecuteAsync(ExecutorContext context, CancellationToken ct)
    {
        while (context.Fetch.Count > 0)
        {
            await FetchAsync(context, ct).ConfigureAwait(false);

            while (context.Compose.TryDequeue(out var current))
            {
                ComposeResult(context, current);
            }
        }
    }

    // note: this is inefficient and we want to group here, for now we just want to get it working.
    private async Task FetchAsync(ExecutorContext context, CancellationToken ct)
    {
        foreach (var workItem in context.Fetch)
        {
            var arguments = workItem.Arguments;
            var selectionResult = new SelectionResult[workItem.SelectionSet.Selections.Count];

            foreach (var requestNode in context.Plan.GetRequestNodes(workItem.SelectionSet))
            {
                var request = requestNode.Handler.CreateRequest(arguments);
                var executor = _executorFactory.Create(requestNode.Handler.SchemaName);
                var result = await executor.ExecuteAsync(request, ct).ConfigureAwait(false);

                // extract arguments

                ExtractSelectionResults(
                    workItem.SelectionSet.Selections,
                    request.SchemaName,
                    result.Data!.Value, // this is wrong, we need a way to correctly extract the result
                    selectionResult);

                // todo: how do we treat errors
            }

            context.Compose.Enqueue(workItem);
        }
    }

    private void ExtractSelectionResults(
        IReadOnlyList<ISelection> selections,
        string schemaName,
        JsonElement data,
        SelectionResult[] selectionResults)
    {
        for (var i = 0; i < selections.Count; i++)
        {
            if (data.TryGetProperty(selections[i].ResponseName, out var property))
            {
                var selectionResult = selectionResults[i];
                if (selectionResult.HasValue)
                {
                    selectionResults[i] = selectionResult.AddResult(new(schemaName, property));
                }
                else
                {
                    selectionResults[i] = new SelectionResult(new JsonResult(schemaName, property));
                }
            }
        }
    }

    private void ComposeResult(
        ExecutorContext context,
        WorkItem workItem)
        => ComposeResult(
            context,
            workItem.SelectionSet.Selections,
            workItem.SelectionResults,
            workItem.Result,
            workItem.Variables);

    private void ComposeResult(
        ExecutorContext context,
        IReadOnlyList<ISelection> selections,
        IReadOnlyList<SelectionResult> selectionResults,
        ObjectResult selectionSetResult,
        ArgumentContext variables)
    {
        for (var i = 0; i < selections.Count; i++)
        {
            var selection = selections[i];
            var selectionResult = selectionResults[i];
            var nullable = selection.TypeKind is not TypeKind.NonNull;
            var namedType = selection.Type.NamedType();

            if (selection.Type.IsScalarType() || namedType.IsScalarType())
            {
                selectionSetResult.SetValueUnsafe(
                    i,
                    selection.ResponseName,
                    selectionResult.Single,
                    nullable);
            }
            else if (selection.Type.IsEnumType() || namedType.IsEnumType())
            {
                // we might need to map the enum value!
                selectionSetResult.SetValueUnsafe(
                    i,
                    selection.ResponseName,
                    selectionResult.Single,
                    nullable);
            }
            else if (selection.Type.IsCompositeType())
            {
                selectionSetResult.SetValueUnsafe(
                    i,
                    selection.ResponseName,
                    ComposeObject(context, selection, selectionResult, variables));
            }
            else
            {
                selectionSetResult.SetValueUnsafe(
                    i,
                    selection.ResponseName,
                    ComposeList(context, selection, selectionResult, variables, selection.Type));
            }
        }
    }

    private ListResult? ComposeList(
        ExecutorContext context,
        ISelection selection,
        SelectionResult selectionResult,
        ArgumentContext variables,
        Types.IType type)
    {
        if (selectionResult.IsNull())
        {
            return null;
        }

        var json = selectionResult.Single.Element;
        var schemaName = selectionResult.Single.SchemaName;
        Debug.Assert(selectionResult.Multiple is null, "selectionResult.Multiple is null");
        Debug.Assert(json.ValueKind is JsonValueKind.Array, "json.ValueKind is JsonValueKind.Array");

        var elementType = type.ElementType();
        var result = context.Result.RentList(json.GetArrayLength());

        if (elementType.IsListType())
        {
            foreach (var item in json.EnumerateArray())
            {
                result.AddUnsafe(
                    ComposeList(
                        context,
                        selection,
                        new SelectionResult(new JsonResult(schemaName, item)),
                        variables,
                        elementType));
            }
        }
        else
        {
            foreach (var item in json.EnumerateArray())
            {
                result.AddUnsafe(
                    ComposeObject(
                        context,
                        selection,
                        new SelectionResult(new JsonResult(schemaName, item)),
                        variables));
            }
        }

        return result;
    }

    private ObjectResult? ComposeObject(
        ExecutorContext context,
        ISelection selection,
        SelectionResult selectionResult,
        ArgumentContext variables)
    {
        if (selectionResult.IsNull())
        {
            return null;
        }

        var typeInfo = selectionResult.GetTypeInfo();
        var typeMetadata = _schema.GetType<ObjectType>(typeInfo);
        var type = context.Schema.GetType<Types.ObjectType>(typeMetadata.Name);
        var selectionSet = context.Operation.GetSelectionSet(selection, type);
        var result = context.Result.RentObject(selectionSet.Selections.Count);

        if (context.RequiresFetch.Contains(selectionSet))
        {
            var fetchArguments = CreateArguments(
                context,
                selection,
                selectionResult,
                typeMetadata);

            var fetchWorkItem = new WorkItem(fetchArguments, selectionSet, variables, result);
            context.Fetch.Add(fetchWorkItem);
        }
        else
        {
            var childSelectionResults = CreateSelectionResults(
                context,
                selection,
                selectionResult,
                typeMetadata);

            var childVariables = CreateVariables(
                context,
                selection,
                selectionResult,
                typeMetadata,
                variables);

            ComposeResult(
                context,
                selectionSet.Selections,
                childSelectionResults,
                result,
                childVariables);
        }

        return result;
    }

    private IReadOnlyList<Argument> CreateArguments(
        ExecutorContext context,
        ISelection selection,
        SelectionResult selectionResult,
        ObjectType typeMetadata)
        => throw new NotImplementedException();

    private ArgumentContext CreateVariables(
        ExecutorContext context,
        ISelection selection,
        SelectionResult selectionResult,
        ObjectType typeMetadata,
        ArgumentContext variables)
        => throw new NotImplementedException();

    private IReadOnlyList<SelectionResult> CreateSelectionResults(
        ExecutorContext context,
        ISelection selection,
        SelectionResult selectionResult,
        ObjectType typeMetadata)
        => throw new NotImplementedException();

    private sealed class ExecutorContext
    {
        public ISchema Schema { get; }

        public ResultBuilder Result { get; }

        public IOperation Operation { get; }

        public QueryPlan Plan { get; }

        public IReadOnlySet<ISelectionSet> RequiresFetch { get; }

        public List<WorkItem> Fetch { get; } = new();

        public Queue<WorkItem> Compose { get; } = new();
    }

    private readonly struct ArgumentContext
    {
        private readonly List<Item> _items;

        private ArgumentContext(List<Item> items)
        {
            _items = items;
        }

        public ArgumentContext Push(IReadOnlyList<Argument> arguments)
        {
            var items = new List<Item>();

            foreach (var argument in arguments)
            {
                items.Add(new Item(0, argument));
            }

            if (_items is not null)
            {
                foreach (var currentItem in _items)
                {
                    if (currentItem.Level > 0)
                    {
                        continue;
                    }

                    var add = true;

                    foreach (var newItem in items)
                    {
                        if (currentItem.Argument.Name.EqualsOrdinal(newItem.Argument.Name))
                        {
                            add = false;
                            break;
                        }
                    }

                    if (add)
                    {
                        items.Add(new Item(currentItem.Level + 1, currentItem.Argument));
                    }
                }
            }

            return new ArgumentContext(items);
        }

        private readonly struct Item
        {
            public Item(int level, Argument argument)
            {
                Level = level;
                Argument = argument;
            }

            public int Level { get; }

            public Argument Argument { get; }
        }
    }

    private struct WorkItem
    {
        public WorkItem(
            ISelectionSet selectionSet,
            ArgumentContext variables,
            ObjectResult result)
            : this(Array.Empty<Argument>(), selectionSet, variables, result) { }

        public WorkItem(
            IReadOnlyList<Argument> arguments,
            ISelectionSet selectionSet,
            ArgumentContext variables,
            ObjectResult result)
        {
            Arguments = arguments;
            SelectionSet = selectionSet;
            SelectionResults = Array.Empty<SelectionResult>();
            Variables = variables;
            Result = result;
        }

        public IReadOnlyList<Argument> Arguments { get; }

        public ISelectionSet SelectionSet { get; }

        public IReadOnlyList<SelectionResult> SelectionResults { get; set; }

        public ArgumentContext Variables { get; set; }

        public ObjectResult Result { get; }
    }
}
