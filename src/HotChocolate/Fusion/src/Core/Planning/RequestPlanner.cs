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

        Plan(
            context,
            backlog,
            selectionSetType,
            selections,
            null,
            false);

        while (backlog.TryDequeue(out var item))
        {
            Plan(
                context,
                backlog,
                item.DeclaringType,
                item.Selections,
                item.ParentSelection,
                item.PreferBatching);
        }
    }

    private void Plan(
        QueryPlanContext context,
        Queue<BacklogItem> backlog,
        ObjectType selectionSetType,
        IReadOnlyList<ISelection> selections,
        ISelection? parentSelection,
        bool preferBatching)
    {
        var variablesInContext = new HashSet<string>();
        List<ISelection>? leftovers = null;

        do
        {
            var current = (IReadOnlyList<ISelection>?)leftovers ?? selections;
            var subgraph = GetBestMatchingSubgraph(context.Operation, current, selectionSetType);
            var workItem = new SelectionExecutionStep(subgraph, selectionSetType, parentSelection);
            leftovers = null;
            ResolverDefinition? resolver;

            if (parentSelection is not null &&
                selectionSetType.Resolvers.ContainsResolvers(subgraph))
            {
                CalculateVariablesInContext(selectionSetType, parentSelection, variablesInContext);
                if (TryGetResolver(
                        selectionSetType,
                        subgraph,
                        variablesInContext,
                        preferBatching,
                        out resolver))
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
                            subgraph,
                            selectionSetType,
                            parentSelection);
                        context.Steps.Add(introspectionStep);
                        context.HasIntrospectionSelections = true;
                    }

                    continue;
                }

                var field = selectionSetType.Fields[selection.Field.Name];
                if (field.Bindings.ContainsSubgraph(subgraph))
                {
                    CalculateVariablesInContext(
                        selection,
                        selectionSetType,
                        parentSelection,
                        variablesInContext);

                    resolver = null;
                    if (field.Resolvers.ContainsResolvers(subgraph))
                    {
                        if (!TryGetResolver(
                                field,
                                subgraph,
                                variablesInContext,
                                preferBatching,
                                out resolver))
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
                        CollectChildSelections(
                            backlog,
                            context.Operation,
                            selection,
                            workItem,
                            preferBatching);
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
        SelectionExecutionStep executionStep,
        bool preferBatching)
    {
        if (!preferBatching)
        {
            preferBatching = Types.TypeExtensions.IsListType(parentSelection.Type);
        }

        foreach (var possibleType in operation.GetPossibleTypes(parentSelection))
        {
            var declaringType = _configuration.GetType<ObjectType>(possibleType.Name);
            var selectionSet = operation.GetSelectionSet(parentSelection, possibleType);
            List<ISelection>? leftovers = null;

            executionStep.AllSelectionSets.Add(selectionSet);

            foreach (var selection in selectionSet.Selections)
            {
                var field = declaringType.Fields[selection.Field.Name];

                if (field.Bindings.TryGetValue(executionStep.SubgraphName, out _))
                {
                    executionStep.AllSelections.Add(selection);

                    if (selection.SelectionSet is not null)
                    {
                        CollectChildSelections(
                            backlog,
                            operation,
                            selection,
                            executionStep,
                            preferBatching);
                    }
                }
                else
                {
                    (leftovers ??= new()).Add(selection);
                }
            }

            if (leftovers is not null)
            {
                backlog.Enqueue(
                    new BacklogItem(
                        parentSelection,
                        declaringType,
                        leftovers,
                        preferBatching));
            }
        }
    }

    private string GetBestMatchingSubgraph(
        IOperation operation,
        IReadOnlyList<ISelection> selections,
        ObjectType typeContext)
    {
        var bestScore = 0;
        var bestSubgraph = _configuration.SubgraphNames[0];

        foreach (var schemaName in _configuration.SubgraphNames)
        {
            var score = CalculateSchemaScore(operation, selections, typeContext, schemaName);

            if (score > bestScore)
            {
                bestScore = score;
                bestSubgraph = schemaName;
            }
        }

        return bestSubgraph;
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
                typeContext.Fields[selection.Field.Name].Bindings.ContainsSubgraph(schemaName))
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
        bool preferBatching,
        [NotNullWhen(true)] out ResolverDefinition? resolver)
        => TryGetResolver(
            field.Resolvers,
            schemaName,
            variablesInContext,
            preferBatching,
            out resolver);

    private static bool TryGetResolver(
        ObjectType declaringType,
        string schemaName,
        HashSet<string> variablesInContext,
        bool preferBatching,
        [NotNullWhen(true)] out ResolverDefinition? resolver)
        => TryGetResolver(
            declaringType.Resolvers,
            schemaName,
            variablesInContext,
            preferBatching,
            out resolver);

    private static bool TryGetResolver(
        ResolverDefinitionCollection resolverDefinitions,
        string schemaName,
        HashSet<string> variablesInContext,
        bool preferBatching,
        [NotNullWhen(true)] out ResolverDefinition? resolver)
    {
        resolver = null;

        if (resolverDefinitions.TryGetValue(schemaName, out var resolvers))
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
                    switch (current.Kind)
                    {
                        case ResolverKind.Batch:
                            resolver = current;
                            if (preferBatching)
                            {
                                return true;
                            }
                            break;

                        case ResolverKind.BatchByKey:
                            resolver = current;
                            break;

                        case ResolverKind.Subscription:
                            throw new NotImplementedException();

                        case ResolverKind.Query:
                        default:
                            if (!preferBatching)
                            {
                                resolver = current;
                                return true;
                            }

                            resolver ??= current;
                            break;
                    }
                }
            }
        }

        return resolver is not null;
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
            IReadOnlyList<ISelection> selections,
            bool preferBatching)
        {
            ParentSelection = parentSelection;
            DeclaringType = declaringType;
            Selections = selections;
            PreferBatching = preferBatching;
        }

        public ISelection ParentSelection { get; }

        public ObjectType DeclaringType { get; }

        public IReadOnlyList<ISelection> Selections { get; }

        public bool PreferBatching { get; }
    }
}
