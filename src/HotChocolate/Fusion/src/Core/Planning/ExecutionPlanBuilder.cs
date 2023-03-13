using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.Metadata.ResolverKind;

namespace HotChocolate.Fusion.Planning;

internal sealed class ExecutionPlanBuilder
{
    private readonly FusionGraphConfiguration _serviceConfig;
    private readonly ISchema _schema;

    public ExecutionPlanBuilder(FusionGraphConfiguration serviceConfig, ISchema schema)
    {
        _serviceConfig = serviceConfig ?? throw new ArgumentNullException(nameof(serviceConfig));
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
    }

    public QueryPlan Build(QueryPlanContext context)
    {
        foreach (var step in context.Steps)
        {
            context.ForwardedVariables.Clear();

            if (step is SelectionExecutionStep selectionStep)
            {
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
            else if (step is IntrospectionExecutionStep)
            {
                var introspectionNode = new IntrospectionNode(
                    context.CreateNodeId(),
                    context.Operation.RootSelectionSet);
                context.Nodes.Add(step, introspectionNode);
                context.HasNodes.Add(context.Operation.RootSelectionSet);
            }
        }

        var rootNode = BuildQueryTree(context);

        return new QueryPlan(
            context.Operation,
            rootNode,
            context.Exports.All
                .GroupBy(t => t.SelectionSet)
                .ToDictionary(t => t.Key, t => t.Select(x => x.StateKey).ToArray()),
            context.HasNodes);
    }

    private QueryPlanNode BuildQueryTree(QueryPlanContext context)
    {
        var completed = new HashSet<IExecutionStep>();
        KeyValuePair<IExecutionStep, QueryPlanNode>[] current;
        QueryPlanNode parent;

        if (context.Operation.Type is not OperationType.Subscription)
        {
            current = context.Nodes.Where(t => t.Key.DependsOn.Count is 0).ToArray();
            parent = new SerialNode(context.CreateNodeId());
        }
        else
        {
            var root = context.Nodes.First(t => t.Value.Kind is QueryPlanNodeKind.Subscription);
            parent = root.Value;

            var selectionSet = ResolveSelectionSet(context, root.Key);
            var compose = new CompositionNode(context.CreateNodeId(), selectionSet);
            parent.AddNode(compose);
            completed.Add(root.Key);
            context.Nodes.Remove(root.Key);

            current = context.Nodes.Where(t => completed.IsSupersetOf(t.Key.DependsOn)).ToArray();
        }

        while (current.Length > 0)
        {
            if (current.Length is 1)
            {
                var node = current[0];
                var selectionSet = ResolveSelectionSet(context, node.Key);
                var compose = new CompositionNode(context.CreateNodeId(), selectionSet);
                parent.AddNode(node.Value);
                parent.AddNode(compose);
                context.Nodes.Remove(node.Key);
                completed.Add(node.Key);
            }
            else
            {
                var parallel = new ParallelNode(context.CreateNodeId());
                var selectionSets = new List<ISelectionSet>();

                foreach (var node in current)
                {
                    selectionSets.Add(ResolveSelectionSet(context, node.Key));
                    parallel.AddNode(node.Value);
                    context.Nodes.Remove(node.Key);
                    completed.Add(node.Key);
                }

                var compose = new CompositionNode(context.CreateNodeId(), selectionSets);

                parent.AddNode(parallel);
                parent.AddNode(compose);
            }

            current = context.Nodes.Where(t => completed.IsSupersetOf(t.Key.DependsOn)).ToArray();
        }

        return parent;
    }

    private ResolverNode CreateResolverNode(
        QueryPlanContext context,
        SelectionExecutionStep executionStep)
    {
        var selectionSet = ResolveSelectionSet(context, executionStep);
        var (requestDocument, path) = CreateRequestDocument(context, executionStep);

        context.HasNodes.Add(selectionSet);

        return new ResolverNode(
            context.CreateNodeId(),
            executionStep.SubgraphName,
            requestDocument,
            selectionSet,
            executionStep.Variables.Values.ToArray(),
            path,
            context.ForwardedVariables.Select(t => t.Variable.Name.Value).ToArray());
    }

    private BatchByKeyResolverNode CreateBatchResolverNode(
        QueryPlanContext context,
        SelectionExecutionStep executionStep,
        ResolverDefinition resolver)
    {
        var selectionSet = ResolveSelectionSet(context, executionStep);
        var (requestDocument, path) = CreateRequestDocument(context, executionStep);

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
            requestDocument,
            selectionSet,
            executionStep.Variables.Values.ToArray(),
            path,
            argumentTypes,
            context.ForwardedVariables.Select(t => t.Variable.Name.Value).ToArray());
    }

    private SubscriptionNode CreateSubscription(
        QueryPlanContext context,
        SelectionExecutionStep executionStep)
    {
        var selectionSet = ResolveSelectionSet(context, executionStep);
        var (requestDocument, path) =
            CreateRequestDocument(
                context,
                executionStep,
                OperationType.Subscription);

        context.HasNodes.Add(selectionSet);

        return new SubscriptionNode(
            context.CreateNodeId(),
            executionStep.SubgraphName,
            requestDocument,
            selectionSet,
            executionStep.Variables.Values.ToArray(),
            path,
            context.ForwardedVariables.Select(t => t.Variable.Name.Value).ToArray());
    }

    private ISelectionSet ResolveSelectionSet(
        QueryPlanContext context,
        IExecutionStep executionStep)
        => executionStep.ParentSelection is null
            ? context.Operation.RootSelectionSet
            : context.Operation.GetSelectionSet(
                executionStep.ParentSelection,
                _schema.GetType<Types.ObjectType>(executionStep.SelectionSetType.Name));

    private (DocumentNode Document, IReadOnlyList<string> Path) CreateRequestDocument(
        QueryPlanContext context,
        SelectionExecutionStep executionStep,
        OperationType operationType = OperationType.Query)
    {
        var rootSelectionSetNode = CreateRootSelectionSetNode(context, executionStep);
        IReadOnlyList<string> path = Array.Empty<string>();

        if (executionStep.Resolver is not null &&
            executionStep.ParentSelection is not null)
        {
            ResolveRequirements(
                context,
                executionStep.ParentSelection,
                executionStep.Resolver,
                executionStep.Variables);

            var (rootResolver, p) = executionStep.Resolver.CreateSelection(
                context.VariableValues,
                rootSelectionSetNode,
                null);

            rootSelectionSetNode = new SelectionSetNode(new[] { rootResolver });
            path = p;
        }

        var operationDefinitionNode = new OperationDefinitionNode(
            null,
            context.CreateRemoteOperationName(),
            operationType,
            context.Exports.CreateVariableDefinitions(
                context.ForwardedVariables,
                executionStep.Variables.Values,
                executionStep.Resolver?.Arguments),
            Array.Empty<DirectiveNode>(),
            rootSelectionSetNode);

        return (new DocumentNode(new[] { operationDefinitionNode }), path);
    }

    private SelectionSetNode CreateRootSelectionSetNode(
        QueryPlanContext context,
        SelectionExecutionStep executionStep)
    {
        var selectionNodes = new List<ISelectionNode>();
        var selectionSet = executionStep.RootSelections[0].Selection.DeclaringSelectionSet;
        var selectionSetType = executionStep.SelectionSetType;
        Debug.Assert(selectionSet is not null);

        // create
        foreach (var rootSelection in executionStep.RootSelections)
        {
            ISelectionNode selectionNode;
            var field = selectionSetType.Fields[rootSelection.Selection.Field.Name];

            if (rootSelection.Resolver is null)
            {
                selectionNode = CreateSelectionNode(
                    context,
                    executionStep,
                    rootSelection.Selection,
                    field);
            }
            else
            {
                SelectionSetNode? selectionSetNode = null;

                if (rootSelection.Selection.SelectionSet is not null)
                {
                    selectionSetNode = CreateSelectionSetNode(
                        context,
                        executionStep,
                        rootSelection.Selection);
                }

                ResolveRequirements(
                    context,
                    rootSelection.Selection,
                    selectionSetType,
                    executionStep.ParentSelection,
                    rootSelection.Resolver,
                    executionStep.Variables);

                var (s, _) = rootSelection.Resolver.CreateSelection(
                    context.VariableValues,
                    selectionSetNode,
                    rootSelection.Selection.ResponseName);
                selectionNode = s;
            }

            if (selectionNode is FieldNode fieldNode &&
                !rootSelection.Selection.ResponseName.EqualsOrdinal(fieldNode.Name.Value))
            {
                selectionNode = fieldNode.WithAlias(
                    new NameNode(rootSelection.Selection.ResponseName));
            }

            selectionNodes.Add(selectionNode);
        }

        // append exports that were required by other execution steps.
        foreach (var selection in context.Exports.GetExportSelections(executionStep, selectionSet))
        {
            selectionNodes.Add(selection);
        }

        return new SelectionSetNode(selectionNodes);
    }

    private ISelectionNode CreateSelectionNode(
        QueryPlanContext context,
        SelectionExecutionStep executionStep,
        ISelection selection,
        ObjectField field)
    {
        SelectionSetNode? selectionSetNode = null;

        if (selection.SelectionSet is not null)
        {
            selectionSetNode = CreateSelectionSetNode(context, executionStep, selection);
        }

        var binding = field.Bindings[executionStep.SubgraphName];

        var alias = !selection.ResponseName.Equals(binding.Name)
            ? new NameNode(selection.ResponseName)
            : null;

        return new FieldNode(
            null,
            new(binding.Name),
            alias,
            selection.SyntaxNode.Required,
            Array.Empty<DirectiveNode>(), // todo : not sure if we should pass down directives.
            selection.SyntaxNode.Arguments,
            selectionSetNode);
    }

    private SelectionSetNode CreateSelectionSetNode(
        QueryPlanContext context,
        SelectionExecutionStep executionStep,
        ISelection parentSelection)
    {
        // TODO : we need to spec inline fragments or a simple selectionsSet depending on pt
        var selectionNodes = new List<ISelectionNode>();
        var possibleTypes = context.Operation.GetPossibleTypes(parentSelection);

        foreach (var possibleType in possibleTypes)
        {
            var typeContext = _serviceConfig.GetType<ObjectType>(possibleType.Name);
            var selectionSet = context.Operation.GetSelectionSet(parentSelection, possibleType);

            foreach (var selection in selectionSet.Selections)
            {
                if (executionStep.AllSelections.Contains(selection))
                {
                    var field = typeContext.Fields[selection.Field.Name];
                    var selectionNode = CreateSelectionNode(
                        context,
                        executionStep,
                        selection,
                        field);
                    selectionNodes.Add(selectionNode);
                }
            }

            // append exports that were required by other execution steps.
            foreach (var selection in
                context.Exports.GetExportSelections(executionStep, selectionSet))
            {
                selectionNodes.Add(selection);
            }
        }

        return new SelectionSetNode(selectionNodes);
    }

    private void ResolveRequirements(
        QueryPlanContext context,
        ISelection parent,
        ResolverDefinition resolver,
        Dictionary<string, string> variableStateLookup)
    {
        context.VariableValues.Clear();

        var parentDeclaringType = _serviceConfig.GetType<ObjectType>(parent.DeclaringType.Name);
        var parentField = parentDeclaringType.Fields[parent.Field.Name];

        foreach (var variable in parentField.Variables)
        {
            if (resolver.Requires.Contains(variable.Name))
            {
                if (variable is ArgumentVariableDefinition argumentVariable)
                {
                    var argumentValue = parent.Arguments[argumentVariable.ArgumentName];
                    context.VariableValues.Add(variable.Name, argumentValue.ValueLiteral!);
                }
                else
                {
                    // TODO : handle this case
                    throw new NotImplementedException();
                }
            }
        }

        foreach (var requirement in resolver.Requires)
        {
            if (!context.VariableValues.ContainsKey(requirement))
            {
                var stateKey = variableStateLookup[requirement];
                context.VariableValues.Add(requirement, new VariableNode(stateKey));
            }
        }
    }

    private void ResolveRequirements(
        QueryPlanContext context,
        ISelection selection,
        ObjectType declaringType,
        ISelection? parent,
        ResolverDefinition resolver,
        Dictionary<string, string> variableStateLookup)
    {
        context.VariableValues.Clear();

        var field = declaringType.Fields[selection.Field.Name];

        foreach (var variable in field.Variables)
        {
            if (resolver.Requires.Contains(variable.Name) &&
                resolver.SubgraphName.Equals(variable.SubgraphName))
            {
                if (variable is ArgumentVariableDefinition argumentVariable)
                {
                    var argumentValue = selection.Arguments[argumentVariable.ArgumentName];
                    context.VariableValues.Add(variable.Name, argumentValue.ValueLiteral!);
                    TryForwardVariable(context, resolver, argumentValue, argumentVariable);
                }
                else
                {
                    // todo : handle this case
                    throw new NotImplementedException();
                }
            }
        }

        if (parent is not null)
        {
            var parentDeclaringType = _serviceConfig.GetType<ObjectType>(parent.DeclaringType.Name);
            var parentField = parentDeclaringType.Fields[parent.Field.Name];

            foreach (var variable in parentField.Variables)
            {
                if (!context.VariableValues.ContainsKey(variable.Name) &&
                    resolver.Requires.Contains(variable.Name))
                {
                    if (variable is ArgumentVariableDefinition argumentVariable)
                    {
                        var argumentValue = parent.Arguments[argumentVariable.ArgumentName];
                        context.VariableValues.Add(variable.Name, argumentValue.ValueLiteral!);
                        TryForwardVariable(context, resolver, argumentValue, argumentVariable);
                    }
                    else
                    {
                        // todo : handle this case
                        throw new NotImplementedException();
                    }
                }
            }
        }

        foreach (var requirement in resolver.Requires)
        {
            if (!context.VariableValues.ContainsKey(requirement))
            {
                var stateKey = variableStateLookup[requirement];
                context.VariableValues.Add(requirement, new VariableNode(stateKey));
            }
        }

        static void TryForwardVariable(
            QueryPlanContext context,
            ResolverDefinition resolver,
            ArgumentValue argumentValue,
            ArgumentVariableDefinition argumentVariable)
        {
            if (argumentValue.ValueLiteral is VariableNode variableValue)
            {
                context.ForwardedVariables.Add(
                    new VariableDefinitionNode(
                        null,
                        variableValue,
                        resolver.Arguments[argumentVariable.ArgumentName],
                        context.Operation.Definition.VariableDefinitions.First(
                                t => t.Variable.Equals(variableValue, SyntaxComparison.Syntax))
                            .DefaultValue,
                        Array.Empty<DirectiveNode>()));
            }
        }
    }
}
