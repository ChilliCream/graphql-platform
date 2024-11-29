using System.Runtime.CompilerServices;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.FusionResources;

namespace HotChocolate.Fusion.Planning.Pipeline;

internal sealed class ExecutionNodeBuilderMiddleware : IQueryPlanMiddleware
{
    private readonly ISchema _schema;
    private readonly DefaultRequestDocumentFormatter _requestFormatter;
    private readonly NodeRequestDocumentFormatter _nodeRequestFormatter;

    public ExecutionNodeBuilderMiddleware(FusionGraphConfiguration configuration, ISchema schema)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(schema);

        _schema = schema;
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

            if (selectionStep.Resolver?.Kind == ResolverKind.Batch)
            {
                var resolve = CreateResolveByKeyBatchNode(context, selectionStep);
                context.RegisterNode(resolve, selectionStep);
            }
            else if (selectionStep.Resolver is null &&
                selectionStep.RootSelections.Count == 1 &&
                selectionStep.RootSelections[0].Resolver?.Kind is ResolverKind.Subscribe)
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
                        Unsafe.As<Selection>(nodeStep.NodeSelection));
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
                    var selectionSet = Unsafe.As<SelectionSet>(context.Operation.RootSelectionSet);
                    var introspectionNode = new Introspect(context.NextNodeId(), selectionSet);
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

        var config = new ResolverNodeBase.Config(
            executionStep.SubgraphName,
            request.Document,
            executionStep.ParentSelection,
            selectionSet,
            executionStep.RootSelections,
            context.Exports.GetExportKeys(executionStep),
            executionStep.Variables.Values,
            context.ForwardedVariables.Select(t => t.Variable.Name.Value),
            request.Path,
            DetermineTransportFeatures(context));

        return new Resolve(context.NextNodeId(), config);
    }

    private TransportFeatures DetermineTransportFeatures(
        QueryPlanContext context)
    {
        if (context.ForwardedVariables.Count == 0 ||
            !_schema.TryGetType<UploadType>("Upload", out _))
        {
            return TransportFeatures.Standard;
        }

        HashSet<InputObjectType>? processed = null;
        Stack<InputObjectType>? next = null;

        foreach (var variable in context.ForwardedVariables)
        {
            var typeName = variable.Type.NamedType().Name.Value;

            if (typeName.EqualsOrdinal("Upload"))
            {
                return TransportFeatures.All;
            }

            if (_schema.TryGetType<InputObjectType>(typeName, out var inputObjectType))
            {
                processed ??= [];
                next ??= new Stack<InputObjectType>();

                processed.Add(inputObjectType);
                next.Push(inputObjectType);

                while (next.TryPop(out var current))
                {
                    foreach (var field in current.Fields)
                    {
                        var fieldType = field.Type.NamedType();

                        if (fieldType is UploadType)
                        {
                            return TransportFeatures.All;
                        }

                        if(fieldType is InputObjectType nextInputObjectType &&
                            processed.Add(nextInputObjectType))
                        {
                            next.Push(nextInputObjectType);
                        }
                    }
                }
            }
        }

        processed?.Clear();
        return TransportFeatures.Standard;
    }

    private Resolve CreateResolveNodeNode(
        QueryPlanContext context,
        NodeEntityExecutionStep executionStep)
    {
        var selectionSet = ResolveSelectionSet(context, executionStep.SelectEntityStep);
        var (requestDocument, path) = _nodeRequestFormatter.CreateRequestDocument(
            context,
            executionStep.SelectEntityStep,
            executionStep.SelectionSetTypeMetadata.Name);

        context.RegisterSelectionSet(selectionSet);

        var config = new ResolverNodeBase.Config(
            executionStep.SelectEntityStep.SubgraphName,
            requestDocument,
            executionStep.ParentSelection,
            selectionSet,
            executionStep.SelectEntityStep.RootSelections,
            context.Exports.GetExportKeys(executionStep),
            executionStep.SelectEntityStep.Variables.Values,
            context.ForwardedVariables.Select(t => t.Variable.Name.Value),
            path,
            DetermineTransportFeatures(context));

        return new Resolve(context.NextNodeId(), config);
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
                if (!executionStep.Variables.TryGetValue(argument.Key, out var stateKey))
                {
                    throw new InvalidOperationException(
                        ExecutionNodeBuilderMiddleware_CreateResolveByKeyBatchNode_StateInconsistent);
                }

                temp.Add(stateKey, argument.Value);
            }

            argumentTypes = temp;
        }

        var config = new ResolverNodeBase.Config(
            executionStep.SubgraphName,
            request.Document,
            executionStep.ParentSelection,
            selectionSet,
            executionStep.RootSelections,
            context.Exports.GetExportKeys(executionStep),
            executionStep.Variables.Values,
            context.ForwardedVariables.Select(t => t.Variable.Name.Value),
            request.Path,
            DetermineTransportFeatures(context));

        return new ResolveByKeyBatch(context.NextNodeId(), config, argumentTypes);
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

        var config = new ResolverNodeBase.Config(
            executionStep.SubgraphName,
            request.Document,
            executionStep.ParentSelection,
            selectionSet,
            executionStep.RootSelections,
            context.Exports.GetExportKeys(executionStep),
            executionStep.Variables.Values,
            context.ForwardedVariables.Select(t => t.Variable.Name.Value),
            request.Path,
            DetermineTransportFeatures(context));

        return new Subscribe(context.NextNodeId(), config);
    }

    private ISelectionSet ResolveSelectionSet(
        QueryPlanContext context,
        SelectionExecutionStep executionStep)
    {
        if (executionStep.Resolver is null &&
            executionStep.SelectionResolvers.Count == 0 &&
            executionStep.ParentSelectionPath is not null)
        {
            return context.Operation.RootSelectionSet;
        }

        return executionStep.ParentSelection is null
            ? context.Operation.RootSelectionSet
            : context.Operation.GetSelectionSet(
                executionStep.ParentSelection,
                _schema.GetType<ObjectType>(executionStep.SelectionSetTypeMetadata.Name));
    }
}
