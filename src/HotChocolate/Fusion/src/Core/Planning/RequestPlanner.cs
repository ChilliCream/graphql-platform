using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// The request planer will rewrite the <see cref="IOperation"/> into
/// queries against the downstream services.
/// </summary>
internal sealed class RequestPlanner
{
    private readonly ServiceConfiguration _serviceConfig;
    private readonly Queue<BacklogItem> _backlog = new(); // TODO: we should get rid of this, maybe put it on the context?

    public RequestPlanner(ServiceConfiguration serviceConfig)
    {
        _serviceConfig = serviceConfig ?? throw new ArgumentNullException(nameof(serviceConfig));
    }

    public void Plan(QueryPlanContext context)
    {
        var selectionSetType = _serviceConfig.GetType<ObjectType>(context.Operation.RootType.Name);
        var selections = context.Operation.RootSelectionSet.Selections;

        Plan(context, selectionSetType, selections, null);

        while (_backlog.TryDequeue(out var item))
        {
            Plan(context, item.DeclaringType, item.Selections, item.ParentSelection);
        }
    }

    private void Plan(
        QueryPlanContext context,
        ObjectType selectionSetType,
        IReadOnlyList<ISelection> selections,
        ISelection? parentSelection)
    {
        var variablesInContext = new HashSet<string>();
        List<ISelection>? leftovers = null;

        do
        {
            var current = (IReadOnlyList<ISelection>?)leftovers ?? selections;
            var schemaName = ResolveBestMatchingSchema(context.Operation, current, selectionSetType);
            var workItem = new SelectionExecutionStep(schemaName, selectionSetType, parentSelection);
            leftovers = null;
            FetchDefinition? resolver;

            if (parentSelection is not null &&
                selectionSetType.Resolvers.ContainsResolvers(schemaName))
            {
                CalculateVariablesInContext(selectionSetType, parentSelection, variablesInContext);
                if (TryGetResolver(selectionSetType, schemaName, variablesInContext, out resolver))
                {
                    workItem.Resolver = resolver;
                    CalculateRequirements(parentSelection, resolver, workItem.Requires);
                }
            }

            foreach (var selection in current)
            {
                if (selection.Field.IsIntrospectionField)
                {
                    if (!context.HasIntrospectionSelections &&
                        (selection.Field.Name.EqualsOrdinal(IntrospectionFields.Schema) ||
                            selection.Field.Name.EqualsOrdinal(IntrospectionFields.Type)))
                    {
                        context.HasIntrospectionSelections = true;
                    }

                    continue;
                }

                var field = selectionSetType.Fields[selection.Field.Name];
                if (field.Bindings.TryGetValue(schemaName, out _))
                {
                    CalculateVariablesInContext(
                        selection,
                        selectionSetType,
                        parentSelection,
                        variablesInContext);

                    resolver = null;
                    if (field.Resolvers.ContainsResolvers(schemaName))
                    {
                        if (!TryGetResolver(field, schemaName, variablesInContext, out resolver))
                        {
                            // todo : error message and type
                            throw new InvalidOperationException(
                                "There must be a field fetch definition valid in this context!");
                        }

                        CalculateRequirements(
                            selection,
                            selectionSetType,
                            parentSelection,
                            resolver,
                            workItem.Requires);
                    }

                    workItem.AllSelections.Add(selection);
                    workItem.RootSelections.Add(new RootSelection(selection, resolver));

                    if (selection.SelectionSet is not null)
                    {
                        CollectChildSelections(context.Operation, selection, workItem);
                    }
                }
                else
                {
                    (leftovers ??= new()).Add(selection);
                }
            }

            if (workItem.RootSelections.Count > 0)
            {
                context.Steps.Add(workItem);
            }

        } while (leftovers is not null);
    }

    private void CollectChildSelections(
        IOperation operation,
        ISelection parentSelection,
        SelectionExecutionStep executionStep)
    {
        foreach (var possibleType in operation.GetPossibleTypes(parentSelection))
        {
            var declaringType = _serviceConfig.GetType<ObjectType>(possibleType.Name);
            var selectionSet = operation.GetSelectionSet(parentSelection, possibleType);
            List<ISelection>? leftovers = null;

            executionStep.AllSelectionSets.Add(selectionSet);

            foreach (var selection in selectionSet.Selections)
            {
                var field = declaringType.Fields[selection.Field.Name];

                if (field.Bindings.TryGetValue(executionStep.SchemaName, out _))
                {
                    executionStep.AllSelections.Add(selection);

                    if (selection.SelectionSet is not null)
                    {
                        CollectChildSelections(operation, selection, executionStep);
                    }
                }
                else
                {
                    (leftovers ??= new()).Add(selection);
                }
            }

            if (leftovers is not null)
            {
                _backlog.Enqueue(new BacklogItem(parentSelection, declaringType, leftovers));
            }
        }
    }

    private string ResolveBestMatchingSchema(
        IOperation operation,
        IReadOnlyList<ISelection> selections,
        ObjectType typeContext)
    {
        var bestScore = 0;
        var bestSchema = _serviceConfig.Bindings[0];

        foreach (var schemaName in _serviceConfig.Bindings)
        {
            var score = CalculateSchemaScore(operation, selections, typeContext, schemaName);

            if (score > bestScore)
            {
                bestScore = score;
                bestSchema = schemaName;
            }
        }

        return bestSchema;
    }

    private int CalculateSchemaScore(
        IOperation operation,
        IReadOnlyList<ISelection> selections,
        ObjectType typeContext,
        string schemaName)
    {
        var score = 0;

        foreach (var selection in selections)
        {
            if (!selection.Field.IsIntrospectionField &&
                typeContext.Fields[selection.Field.Name].Bindings.ContainsSchema(schemaName))
            {
                score++;

                if (selection.SelectionSet is not null)
                {
                    foreach (var possibleType in operation.GetPossibleTypes(selection))
                    {
                        var type = _serviceConfig.GetType<ObjectType>(possibleType.Name);
                        var selectionSet = operation.GetSelectionSet(selection, possibleType);
                        score += CalculateSchemaScore(
                            operation,
                            selectionSet.Selections,
                            type,
                            schemaName);
                    }
                }
            }
        }

        return score;
    }

    private static bool TryGetResolver(
        ObjectField field,
        string schemaName,
        HashSet<string> variablesInContext,
        [NotNullWhen(true)] out FetchDefinition? resolver)
    {
        if (field.Resolvers.TryGetValue(schemaName, out var resolvers))
        {
            foreach (var current in resolvers)
            {
                var canBeUsed = true;

                foreach (var requirement in current.Requires)
                {
                    if (!variablesInContext.Contains(requirement))
                    {
                        canBeUsed = false;
                        break;
                    }
                }

                if (canBeUsed)
                {
                    resolver = current;
                    return true;
                }
            }
        }

        resolver = null;
        return false;
    }

    private static bool TryGetResolver(
        ObjectType declaringType,
        string schemaName,
        HashSet<string> variablesInContext,
        [NotNullWhen(true)] out FetchDefinition? resolver)
    {
        if (declaringType.Resolvers.TryGetValue(schemaName, out var resolvers))
        {
            foreach (var current in resolvers)
            {
                var canBeUsed = true;

                foreach (var requirement in current.Requires)
                {
                    if (!variablesInContext.Contains(requirement))
                    {
                        canBeUsed = false;
                        break;
                    }
                }

                if (canBeUsed)
                {
                    resolver = current;
                    return true;
                }
            }
        }

        resolver = null;
        return false;
    }

    private void CalculateVariablesInContext(
        ISelection selection,
        ObjectType declaringType,
        ISelection? parent,
        HashSet<string> variablesInContext)
    {
        variablesInContext.Clear();

        if (parent is not null)
        {
            var parentDeclaringType = _serviceConfig.GetType<ObjectType>(parent.DeclaringType.Name);
            var parentField = parentDeclaringType.Fields[parent.Field.Name];

            foreach (var variable in parentField.Variables)
            {
                variablesInContext.Add(variable.Name);
            }
        }

        foreach (var variable in declaringType.Variables)
        {
            variablesInContext.Add(variable.Name);
        }

        var field = declaringType.Fields[selection.Field.Name];

        foreach (var variable in field.Variables)
        {
            variablesInContext.Add(variable.Name);
        }
    }

    private void CalculateVariablesInContext(
        ObjectType declaringType,
        ISelection parent,
        HashSet<string> variablesInContext)
    {
        variablesInContext.Clear();

        var parentDeclaringType = _serviceConfig.GetType<ObjectType>(parent.DeclaringType.Name);
        var parentField = parentDeclaringType.Fields[parent.Field.Name];

        foreach (var variable in parentField.Variables)
        {
            variablesInContext.Add(variable.Name);
        }

        foreach (var variable in declaringType.Variables)
        {
            variablesInContext.Add(variable.Name);
        }
    }

    private void CalculateRequirements(
        ISelection selection,
        ObjectType declaringType,
        ISelection? parent,
        FetchDefinition resolver,
        HashSet<string> requirements)
    {
        var field = declaringType.Fields[selection.Field.Name];
        var inContext = field.Variables.Select(t => t.Name);

        if (parent is not null)
        {
            var parentDeclaringType = _serviceConfig.GetType<ObjectType>(parent.DeclaringType.Name);
            var parentField = parentDeclaringType.Fields[parent.Field.Name];
            inContext = inContext.Concat(parentField.Variables.Select(t => t.Name));
        }

        foreach (var requirement in resolver.Requires.Except(inContext))
        {
            requirements.Add(requirement);
        }
    }

    private void CalculateRequirements(
        ISelection parent,
        FetchDefinition resolver,
        HashSet<string> requirements)
    {
        var parentDeclaringType = _serviceConfig.GetType<ObjectType>(parent.DeclaringType.Name);
        var parentField = parentDeclaringType.Fields[parent.Field.Name];

        foreach (var requirement in
            resolver.Requires.Except(parentField.Variables.Select(t => t.Name)))
        {
            requirements.Add(requirement);
        }
    }

    private readonly struct BacklogItem
    {
        public BacklogItem(
            ISelection parentSelection,
            ObjectType declaringType,
            IReadOnlyList<ISelection> selections)
        {
            ParentSelection = parentSelection;
            DeclaringType = declaringType;
            Selections = selections;
        }

        public ISelection ParentSelection { get; }

        public ObjectType DeclaringType { get; }

        public IReadOnlyList<ISelection> Selections { get; }
    }
}
