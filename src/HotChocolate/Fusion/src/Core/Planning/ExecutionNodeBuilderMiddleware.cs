using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Language;
using static HotChocolate.Fusion.Metadata.ResolverKind;

namespace HotChocolate.Fusion.Planning;

internal sealed class ExecutionNodeBuilderMiddleware : IQueryPlanMiddleware
{
    private readonly ISchema _schema;
    private readonly DefaultRequestDocumentFormatter _requestFormatter;
    private readonly NodeRequestDocumentFormatter _nodeRequestFormatter;

    public ExecutionNodeBuilderMiddleware(FusionGraphConfiguration configuration, ISchema schema)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        _requestFormatter = new DefaultRequestDocumentFormatter(configuration);
        _nodeRequestFormatter = new NodeRequestDocumentFormatter(configuration, schema);
    }

    public void Invoke(QueryPlanContext context, QueryPlanDelegate next)
    {
        var executionSteps = context.Steps;

        HandledSpecialQueryFields(context, ref executionSteps);

        foreach (var step in executionSteps)
        {
            context.ForwardedVariables.Clear();

            if (step is not SelectionExecutionStep selectionStep)
            {
                continue;
            }

            if (selectionStep.Resolver?.Kind == BatchByKey)
            {
                var resolve = CreateResolveByKeyBatchNode(context, selectionStep);
                context.RegisterNode(resolve, selectionStep);
            }
            else if (selectionStep.Resolver is null &&
                selectionStep.RootSelections.Count == 1 &&
                selectionStep.RootSelections[0].Resolver?.Kind is Subscription)
            {
                var resolve = CreateSubscribeNode(context, selectionStep);
                context.RegisterNode(resolve, selectionStep);
            }
            else
            {
                var resolve = CreateResolveNode(context, selectionStep);
                context.RegisterNode(resolve, selectionStep);
            }
        }

        next(context);
    }

    private void HandledSpecialQueryFields(
        QueryPlanContext context,
        ref List<ExecutionStep> executionSteps)
    {
        if (context.HasHandledSpecialQueryFields)
        {
            var handled = new HashSet<ExecutionStep>();

            foreach (var executionStep in executionSteps)
            {
                if (executionStep is NodeExecutionStep nodeStep)
                {
                    var nodeResolverNode = new ResolveNode(
                        context.NextNodeId(),
                        nodeStep.NodeSelection);
                    context.RegisterNode(nodeResolverNode, nodeStep);
                    context.RegisterSelectionSet(context.Operation.RootSelectionSet);
                    handled.Add(nodeStep);

                    foreach (var entityStep in nodeStep.EntitySteps)
                    {
                        context.ForwardedVariables.Clear();

                        var resolve = CreateResolveNodeNode(context, entityStep);
                        nodeResolverNode.AddEntityResolver(entityStep.TypeName, resolve);
                        context.RegisterNode(resolve, entityStep.SelectEntityStep);

                        handled.Add(entityStep);
                        handled.Add(entityStep.SelectEntityStep);
                    }
                }

                if (executionStep is IntrospectionExecutionStep)
                {
                    var introspectionNode = new Introspect(
                        context.NextNodeId(),
                        context.Operation.RootSelectionSet);
                    context.RegisterNode(introspectionNode, executionStep);
                    context.RegisterSelectionSet(context.Operation.RootSelectionSet);
                    handled.Add(executionStep);
                }
            }

            if (executionSteps.Count == handled.Count)
            {
                executionSteps.Clear();
            }
            else
            {
                executionSteps = executionSteps.Where(t => !handled.Contains(t)).ToList();
            }
        }
    }

    private Resolve CreateResolveNode(
        QueryPlanContext context,
        SelectionExecutionStep executionStep)
    {
        var type = OperationType.Query;

        if (executionStep.ParentSelection is null &&
            context.Operation.Type is OperationType.Mutation)
        {
            type = OperationType.Mutation;
        }

        var selectionSet = ResolveSelectionSet(context, executionStep);
        var request = _requestFormatter.CreateRequestDocument(context, executionStep, type);

        context.RegisterSelectionSet(selectionSet);

        return new Resolve(
            context.NextNodeId(),
            executionStep.SubgraphName,
            request.Document,
            selectionSet,
            executionStep.Variables.Values.ToArray(),
            request.Path,
            context.ForwardedVariables.Select(t => t.Variable.Name.Value).ToArray());
    }

    private Resolve CreateResolveNodeNode(
        QueryPlanContext context,
        NodeEntityExecutionStep executionStep)
    {
        var selectionSet = ResolveSelectionSet(context, executionStep.SelectEntityStep);
        var (requestDocument, path) = _nodeRequestFormatter.CreateRequestDocument(
            context,
            executionStep.SelectEntityStep,
            executionStep.SelectionSetTypeInfo.Name);

        context.RegisterSelectionSet(selectionSet);

        return new Resolve(
            context.NextNodeId(),
            executionStep.SelectEntityStep.SubgraphName,
            requestDocument,
            selectionSet,
            executionStep.SelectEntityStep.Variables.Values.ToArray(),
            path,
            context.ForwardedVariables.Select(t => t.Variable.Name.Value).ToArray());
    }

    private ResolveByKeyBatch CreateResolveByKeyBatchNode(
        QueryPlanContext context,
        SelectionExecutionStep executionStep)
    {
        var selectionSet = ResolveSelectionSet(context, executionStep);
        var request = _requestFormatter.CreateRequestDocument(context, executionStep);

        context.RegisterSelectionSet(selectionSet);

        var argumentTypes = executionStep.Resolver!.ArgumentTypes;

        if (argumentTypes.Count > 0)
        {
            var temp = new Dictionary<string, ITypeNode>();

            foreach (var argument in executionStep.Resolver!.ArgumentTypes)
            {
                if (!context.Exports.TryGetStateKey(
                    context.Operation.GetSelectionSet(executionStep),
                    argument.Key,
                    out var stateKey,
                    out _))
                {
                    // TODO : Exception
                    throw new InvalidOperationException("The state is inconsistent.");
                }

                temp.Add(stateKey, argument.Value);
            }

            argumentTypes = temp;
        }

        return new ResolveByKeyBatch(
            context.NextNodeId(),
            executionStep.SubgraphName,
            request.Document,
            selectionSet,
            executionStep.Variables.Values.ToArray(),
            request.Path,
            argumentTypes,
            context.ForwardedVariables.Select(t => t.Variable.Name.Value).ToArray());
    }

    private Subscribe CreateSubscribeNode(
        QueryPlanContext context,
        SelectionExecutionStep executionStep)
    {
        var selectionSet = ResolveSelectionSet(context, executionStep);
        var request =
            _requestFormatter.CreateRequestDocument(
                context,
                executionStep,
                OperationType.Subscription);

        context.RegisterSelectionSet(selectionSet);

        return new Subscribe(
            context.NextNodeId(),
            executionStep.SubgraphName,
            request.Document,
            selectionSet,
            executionStep.Variables.Values.ToArray(),
            request.Path,
            context.ForwardedVariables.Select(t => t.Variable.Name.Value).ToArray());
    }

    private ISelectionSet ResolveSelectionSet(
        QueryPlanContext context,
        ExecutionStep executionStep)
        => executionStep.ParentSelection is null
            ? context.Operation.RootSelectionSet
            : context.Operation.GetSelectionSet(
                executionStep.ParentSelection,
                _schema.GetType<Types.ObjectType>(executionStep.SelectionSetTypeInfo.Name));
}
