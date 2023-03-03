using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// The request planer will rewrite the <see cref="IOperation"/> into
/// requests against the downstream services.
/// </summary>
internal sealed class RequestPlanner
{
    private readonly FusionGraphConfiguration _configuration;

    public RequestPlanner(FusionGraphConfiguration configuration)
    {
        _configuration = configuration ??
            throw new ArgumentNullException(nameof(configuration));
    }

    public void Plan(QueryPlanContext context)
    {
        var selectionSetType = _configuration.GetType<ObjectType>(context.Operation.RootType.Name);
        var selections = context.Operation.RootSelectionSet.Selections;
        var backlog = new Queue<BacklogItem>();

        Plan(context, backlog, selectionSetType, selections, null);

        while (backlog .TryDequeue(out var item))
        {
            Plan(context, backlog, item.DeclaringType, item.Selections, item.ParentSelection);
        }
    }

    private void Plan(
        QueryPlanContext context,
        Queue<BacklogItem> backlog,
        ObjectType selectionSetType,
        IReadOnlyList<ISelection> selections,
        ISelection? parentSelection)
    {
        var variablesInContext = new HashSet<string>();
        List<ISelection>? leftovers = null;

        do
        {
            var current = (IReadOnlyList<ISelection>?)leftovers ?? selections;
            var subGraph = ResolveBestMatchingSubGraph(context.Operation, current, selectionSetType);
            var workItem = new SelectionExecutionStep(subGraph, selectionSetType, parentSelection);
            leftovers = null;
            ResolverDefinition? resolver;

            if (parentSelection is not null &&
                selectionSetType.Resolvers.ContainsResolvers(subGraph))
            {
                CalculateVariablesInContext(selectionSetType, parentSelection, variablesInContext);
                if (TryGetResolver(selectionSetType, subGraph, variablesInContext, out resolver))
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
                        var introspectionStep = new IntrospectionExecutionStep(
                            subGraph,
                            selectionSetType,
                            parentSelection);
                        context.Steps.Add(introspectionStep);
                        context.HasIntrospectionSelections = true;
                    }

                    continue;
                }

                var field = selectionSetType.Fields[selection.Field.Name];
                if (field.Bindings.ContainsSubGraph(subGraph))
                {
                    CalculateVariablesInContext(
                        selection,
                        selectionSetType,
                        parentSelection,
                        variablesInContext);

                    resolver = null;
                    if (field.Resolvers.ContainsResolvers(subGraph))
                    {
                        if (!TryGetResolver(field, subGraph, variablesInContext, out resolver))
                        {
                            // todo : error message and type
                            throw new InvalidOperationException(
                                "There must be a field resolver definition valid in this context!");
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
                        CollectChildSelections(backlog, context.Operation, selection, workItem);
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
        Queue<BacklogItem> backlog,
        IOperation operation,
        ISelection parentSelection,
        SelectionExecutionStep executionStep)
    {
        foreach (var possibleType in operation.GetPossibleTypes(parentSelection))
        {
            var declaringType = _configuration.GetType<ObjectType>(possibleType.Name);
            var selectionSet = operation.GetSelectionSet(parentSelection, possibleType);
            List<ISelection>? leftovers = null;

            executionStep.AllSelectionSets.Add(selectionSet);

            foreach (var selection in selectionSet.Selections)
            {
                var field = declaringType.Fields[selection.Field.Name];

                if (field.Bindings.TryGetValue(executionStep.SubGraphName, out _))
                {
                    executionStep.AllSelections.Add(selection);

                    if (selection.SelectionSet is not null)
                    {
                        CollectChildSelections(backlog, operation, selection, executionStep);
                    }
                }
                else
                {
                    (leftovers ??= new()).Add(selection);
                }
            }

            if (leftovers is not null)
            {
                backlog.Enqueue(new BacklogItem(parentSelection, declaringType, leftovers));
            }
        }
    }

    private string ResolveBestMatchingSubGraph(
        IOperation operation,
        IReadOnlyList<ISelection> selections,
        ObjectType typeContext)
    {
        var bestScore = 0;
        var bestSubGraph = _configuration.SubGraphNames[0];

        foreach (var schemaName in _configuration.SubGraphNames)
        {
            var score = CalculateSchemaScore(operation, selections, typeContext, schemaName);

            if (score > bestScore)
            {
                bestScore = score;
                bestSubGraph = schemaName;
            }
        }

        return bestSubGraph;
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
                typeContext.Fields[selection.Field.Name].Bindings.ContainsSubGraph(schemaName))
            {
                score++;

                if (selection.SelectionSet is not null)
                {
                    foreach (var possibleType in operation.GetPossibleTypes(selection))
                    {
                        var type = _configuration.GetType<ObjectType>(possibleType.Name);
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
        [NotNullWhen(true)] out ResolverDefinition? resolver)
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
        [NotNullWhen(true)] out ResolverDefinition? resolver)
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
            var parentDeclaringType = _configuration.GetType<ObjectType>(parent.DeclaringType.Name);
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

        var parentDeclaringType = _configuration.GetType<ObjectType>(parent.DeclaringType.Name);
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
        ResolverDefinition resolver,
        HashSet<string> requirements)
    {
        var field = declaringType.Fields[selection.Field.Name];
        var inContext = field.Variables.Select(t => t.Name);

        if (parent is not null)
        {
            var parentDeclaringType = _configuration.GetType<ObjectType>(parent.DeclaringType.Name);
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
        ResolverDefinition resolver,
        HashSet<string> requirements)
    {
        var parentDeclaringType = _configuration.GetType<ObjectType>(parent.DeclaringType.Name);
        var parentField = parentDeclaringType.Fields[parent.Field.Name];
        var parentState = parentField.Variables.Select(t => t.Name);

        foreach (var requirement in resolver.Requires.Except(parentState))
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
