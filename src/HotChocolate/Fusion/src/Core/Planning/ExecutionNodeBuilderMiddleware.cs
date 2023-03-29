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
                var fetchNode = CreateBatchResolverNode(
                    context,
                    selectionStep,
                    selectionStep.Resolver);
                context.Nodes.Add(selectionStep, fetchNode);
            }
            else if (selectionStep.Resolver is null &&
                selectionStep.RootSelections.Count == 1 &&
                selectionStep.RootSelections[0].Resolver?.Kind is Subscription)
            {
                var fetchNode = CreateSubscription(context, selectionStep);
                context.Nodes.Add(selectionStep, fetchNode);
            }
            else
            {
                var fetchNode = CreateResolverNode(context, selectionStep);
                context.Nodes.Add(selectionStep, fetchNode);
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
                    var nodeResolverNode = new NodeResolverNode(
                        context.CreateNodeId(),
                        nodeStep.NodeSelection);
                    context.Nodes.Add(nodeStep, nodeResolverNode);
                    context.HasNodes.Add(context.Operation.RootSelectionSet);
                    handled.Add(nodeStep);

                    foreach (var entityStep in nodeStep.EntitySteps)
                    {
                        context.ForwardedVariables.Clear();

                        var fetchNode = CreateNodeResolverNode(context, entityStep);

                        var serialNode = new SerialNode(context.CreateNodeId());
                        serialNode.AddNode(fetchNode);

                        nodeResolverNode.AddNode(entityStep.SelectionSetTypeInfo.Name, serialNode);

                        context.Nodes.Add(entityStep.EntitySelectionExecutionStep, fetchNode);
                        handled.Add(entityStep);
                        handled.Add(entityStep.EntitySelectionExecutionStep);
                    }
                }

                if (executionStep is IntrospectionExecutionStep)
                {
                    var introspectionNode = new IntrospectionNode(
                        context.CreateNodeId(),
                        context.Operation.RootSelectionSet);
                    context.Nodes.Add(executionStep, introspectionNode);
                    context.HasNodes.Add(context.Operation.RootSelectionSet);
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

    private ResolverNode CreateResolverNode(
        QueryPlanContext context,
        SelectionExecutionStep executionStep)
    {
        var selectionSet = ResolveSelectionSet(context, executionStep);
        var request = _requestFormatter.CreateRequestDocument(context, executionStep);

        context.HasNodes.Add(selectionSet);

        return new ResolverNode(
            context.CreateNodeId(),
            executionStep.SubgraphName,
            request.Document,
            selectionSet,
            executionStep.Variables.Values.ToArray(),
            request.Path,
            context.ForwardedVariables.Select(t => t.Variable.Name.Value).ToArray());
    }

    private ResolverNode CreateNodeResolverNode(
        QueryPlanContext context,
        NodeEntityExecutionStep executionStep)
    {
        var selectionSet = ResolveSelectionSet(context, executionStep.EntitySelectionExecutionStep);
        var (requestDocument, path) = _nodeRequestFormatter.CreateRequestDocument(
            context,
            executionStep.EntitySelectionExecutionStep,
            executionStep.SelectionSetTypeInfo.Name);

        context.HasNodes.Add(selectionSet);

        return new ResolverNode(
            context.CreateNodeId(),
            executionStep.EntitySelectionExecutionStep.SubgraphName,
            requestDocument,
            selectionSet,
            executionStep.EntitySelectionExecutionStep.Variables.Values.ToArray(),
            path,
            context.ForwardedVariables.Select(t => t.Variable.Name.Value).ToArray());
    }

    private BatchByKeyResolverNode CreateBatchResolverNode(
        QueryPlanContext context,
        SelectionExecutionStep executionStep,
        ResolverDefinition resolver)
    {
        var selectionSet = ResolveSelectionSet(context, executionStep);
        var request = _requestFormatter.CreateRequestDocument(context, executionStep);

        context.HasNodes.Add(selectionSet);

        var argumentTypes = resolver.Arguments;

        if (argumentTypes.Count > 0)
        {
            var temp = new Dictionary<string, ITypeNode>();

            foreach (var argument in resolver.Arguments)
            {
                if (!context.Exports.TryGetStateKey(
                    executionStep.RootSelections[0].Selection.DeclaringSelectionSet,
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

        return new BatchByKeyResolverNode(
            context.CreateNodeId(),
            executionStep.SubgraphName,
            request.Document,
            selectionSet,
            executionStep.Variables.Values.ToArray(),
            request.Path,
            argumentTypes,
            context.ForwardedVariables.Select(t => t.Variable.Name.Value).ToArray());
    }

    private SubscriptionNode CreateSubscription(
        QueryPlanContext context,
        SelectionExecutionStep executionStep)
    {
        var selectionSet = ResolveSelectionSet(context, executionStep);
        var request =
            _requestFormatter.CreateRequestDocument(
                context,
                executionStep,
                OperationType.Subscription);

        context.HasNodes.Add(selectionSet);

        return new SubscriptionNode(
            context.CreateNodeId(),
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
