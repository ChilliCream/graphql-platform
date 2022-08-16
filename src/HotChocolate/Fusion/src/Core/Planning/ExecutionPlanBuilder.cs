using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

internal sealed class ExecutionPlanBuilder
{
    private readonly ServiceConfiguration _serviceConfig;
    private readonly ISchema _schema;

    public ExecutionPlanBuilder(ServiceConfiguration serviceConfig, ISchema schema)
    {
        _serviceConfig = serviceConfig ?? throw new ArgumentNullException(nameof(serviceConfig));
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
    }

    public QueryPlan Build(QueryPlanContext context)
    {
        foreach (var step in context.Steps)
        {
            if (step is SelectionExecutionStep executionStep)
            {
                var requestNode = CreateRequestNode(context, executionStep);
                context.RequestNodes.Add(executionStep, requestNode);
            }
        }

        foreach (var (step, node) in context.RequestNodes)
        {
            if (step.DependsOn.Count > 0)
            {
                foreach (var dependency in step.DependsOn)
                {
                    node.AddDependency(context.RequestNodes[dependency]);
                }
            }
        }

        return new QueryPlan(context.RequestNodes.Values, context.Exports.All);
    }

    private RequestNode CreateRequestNode(
        QueryPlanContext context,
        SelectionExecutionStep executionStep)
    {
        var selectionSet = executionStep.ParentSelection is null
            ? context.Operation.RootSelectionSet
            : context.Operation.GetSelectionSet(
                executionStep.ParentSelection,
                _schema.GetType<Types.ObjectType>(executionStep.SelectionSetType.Name));

        var (requestDocument, path) = CreateRequestDocument(context, executionStep);

        var requestHandler = new RequestHandler(
            executionStep.SchemaName,
            requestDocument,
            selectionSet,
            // do we need the type?
            executionStep.Variables.Values
                .Select(t => new RequiredState(t, null!, false))
                .ToArray(),
            path);

        return new RequestNode(requestHandler);
    }

    private (DocumentNode Document, IReadOnlyList<string> Path) CreateRequestDocument(
        QueryPlanContext context,
        SelectionExecutionStep executionStep)
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
            OperationType.Query,
            context.Exports.CreateVariableDefinitions(executionStep.Variables.Values),
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

        var binding = field.Bindings[executionStep.SchemaName];

        var alias = !selection.ResponseName.Equals(binding.Name)
            ? new NameNode(selection.ResponseName)
            : null;

        return new FieldNode(
            null,
            new(binding.Name),
            alias,
            null,
            Array.Empty<DirectiveNode>(),
            Array.Empty<ArgumentNode>(),
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
        FetchDefinition resolver,
        Dictionary<string, string> variableStateLookup)
    {
        context.VariableValues.Clear();

        var parentDeclaringType = _serviceConfig.GetType<ObjectType>(parent.DeclaringType.Name);
        var parentField = parentDeclaringType.Fields[parent.Field.Name];

        foreach (var variable in parentField.Variables)
        {
            if (resolver.Requires.Contains(variable.Name))
            {
                var argumentValue = parent.Arguments[variable.ArgumentName];
                context.VariableValues.Add(variable.Name, argumentValue.ValueLiteral!);
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
        FetchDefinition resolver,
        Dictionary<string, string> variableStateLookup)
    {
        context.VariableValues.Clear();

        var field = declaringType.Fields[selection.Field.Name];

        foreach (var variable in field.Variables)
        {
            if (resolver.Requires.Contains(variable.Name))
            {
                var argumentValue = selection.Arguments[variable.ArgumentName];
                context.VariableValues.Add(variable.Name, argumentValue.ValueLiteral!);
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
                    var argumentValue = parent.Arguments[variable.ArgumentName];
                    context.VariableValues.Add(variable.Name, argumentValue.ValueLiteral!);
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
}
