using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities;
using ObjectField = HotChocolate.Fusion.Metadata.ObjectField;
using ObjectType = HotChocolate.Fusion.Metadata.ObjectType;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// The request planer will rewrite the <see cref="IOperation"/> into
/// execution steps that outline the rough structure of the execution
/// plan.
/// </summary>
internal sealed class ExecutionStepDiscoveryMiddleware : IQueryPlanMiddleware
{
    private readonly FusionGraphConfiguration _configuration;

    /// <summary>
    /// Creates a new instance of <see cref="ExecutionStepDiscoveryMiddleware"/>.
    /// </summary>
    /// <param name="configuration">
    /// The fusion gateway configuration.
    /// </param>
    /// <exception cref="ArgumentNullException"></exception>
    public ExecutionStepDiscoveryMiddleware(FusionGraphConfiguration configuration)
    {
        _configuration = configuration ??
            throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Rewrites the <see cref="IOperation"/> into
    /// execution steps that outline the rough structure of the query
    /// plan.
    /// </summary>
    /// <param name="context">
    /// The query plan context.
    /// </param>
    /// <param name="next">
    /// The next middleware in the pipeline.
    /// </param>
    public void Invoke(QueryPlanContext context, QueryPlanDelegate next)
    {
        Plan(context);

        next(context);
    }

    private void Plan(QueryPlanContext context)
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
        var operation = context.Operation;
        List<ISelection>? leftovers = null;

        do
        {
            var current = (IReadOnlyList<ISelection>?)leftovers ?? selections;
            var subgraph = GetBestMatchingSubgraph(operation, current, selectionSetType);
            var executionStep = new DefaultExecutionStep(
                subgraph,
                selectionSetType,
                parentSelection);
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
                    executionStep.Resolver = resolver;
                    CalculateRequirements(parentSelection, resolver, executionStep.Requires);
                }
            }

            foreach (var selection in current)
            {
                var field = selection.Field;

                if (field.IsIntrospectionField)
                {
                    TryCreateIntrospectionExecutionStep(
                        context,
                        field,
                        selectionSetType);
                    continue;
                }

                if (IsNodeField(field, operation))
                {
                    CreateNodeExecutionStep(
                        context,
                        backlog,
                        selection,
                        selectionSetType);
                    continue;
                }

                var fieldInfo = selectionSetType.Fields[field.Name];

                if (fieldInfo.Bindings.ContainsSubgraph(subgraph))
                {
                    CalculateVariablesInContext(
                        selection,
                        selectionSetType,
                        parentSelection,
                        variablesInContext);

                    resolver = null;

                    if (fieldInfo.Resolvers.ContainsResolvers(subgraph))
                    {
                        var resolverPreference =
                            DetermineResolverPreference(
                                operation,
                                parentSelection,
                                preferBatching);

                        if (!TryGetResolver(
                            fieldInfo,
                            subgraph,
                            variablesInContext,
                            resolverPreference,
                            out resolver))
                        {
                            throw ThrowHelper.NoResolverInContext();
                        }

                        CalculateRequirements(
                            selection,
                            selectionSetType,
                            parentSelection,
                            resolver,
                            executionStep.Requires);
                    }

                    executionStep.AllSelections.Add(selection);
                    executionStep.RootSelections.Add(new RootSelection(selection, resolver));

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

            if (executionStep.RootSelections.Count > 0)
            {
                context.Steps.Add(executionStep);
            }
        } while (leftovers is not null);
    }

    private void CollectChildSelections(
        Queue<BacklogItem> backlog,
        IOperation operation,
        ISelection parentSelection,
        DefaultExecutionStep executionStep,
        bool preferBatching)
    {
        if (!preferBatching)
        {
            preferBatching = parentSelection.Type.IsListType();
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

                if (field.Bindings.TryGetBinding(executionStep.SubgraphName, out _))
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

    private static void TryCreateIntrospectionExecutionStep(
        QueryPlanContext context,
        IObjectField field,
        ObjectType queryType)
    {
        if (!context.HasIntrospectionSelections &&
            (field.Name.EqualsOrdinal(IntrospectionFields.Schema) ||
                field.Name.EqualsOrdinal(IntrospectionFields.Type)))
        {
            context.Steps.Add(new IntrospectionExecutionStep(queryType));
            context.HasIntrospectionSelections = true;
        }
    }

    private void CreateNodeExecutionStep(
        QueryPlanContext context,
        Queue<BacklogItem> backlog,
        ISelection nodeSelection,
        ObjectType queryType
    )
    {
        var operation = context.Operation;
        var nodeExecutionStep = new NodeExecutionStep(nodeSelection, queryType);
        context.Steps.Add(nodeExecutionStep);

        foreach (var possibleType in operation.GetPossibleTypes(nodeSelection))
        {
            var declaringType = _configuration.GetType<ObjectType>(possibleType.Name);
            var selectionSet = operation.GetSelectionSet(nodeSelection, possibleType);

            backlog.Enqueue(
                new BacklogItem(
                    nodeSelection,
                    declaringType,
                    selectionSet.Selections,
                    false));
        }
    }

    private static PreferredResolverKind DetermineResolverPreference(
        IOperation operation,
        ISelection? parentSelection,
        bool preferBatching)
        => operation.Type is OperationType.Subscription &&
            parentSelection is null
                ? PreferredResolverKind.Subscription
                : preferBatching
                    ? PreferredResolverKind.Batch
                    : PreferredResolverKind.Query;

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
        var stack = new Stack<(IReadOnlyList<ISelection> selections, ObjectType typeContext)>();
        stack.Push((selections, typeContext));

        while (stack.Count > 0)
        {
            var (currentSelections, currentTypeContext) = stack.Pop();

            foreach (var selection in currentSelections)
            {
                if (!selection.Field.IsIntrospectionField &&
                    currentTypeContext.Fields[selection.Field.Name].Bindings
                        .ContainsSubgraph(schemaName))
                {
                    score++;

                    if (selection.SelectionSet is not null)
                    {
                        foreach (var possibleType in operation.GetPossibleTypes(selection))
                        {
                            var type = _configuration.GetType<ObjectType>(possibleType.Name);
                            var selectionSet = operation.GetSelectionSet(selection, possibleType);
                            stack.Push((selectionSet.Selections, type));
                        }
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
        PreferredResolverKind preference,
        [NotNullWhen(true)] out ResolverDefinition? resolver)
        => TryGetResolver(
            field.Resolvers,
            schemaName,
            variablesInContext,
            preference,
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
            preferBatching
                ? PreferredResolverKind.Batch
                : PreferredResolverKind.Query,
            out resolver);

    private static bool TryGetResolver(
        ResolverDefinitionCollection resolverDefinitions,
        string schemaName,
        HashSet<string> variablesInContext,
        PreferredResolverKind preference,
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
                    if (preference is PreferredResolverKind.Subscription)
                    {
                        if (current.Kind is ResolverKind.Subscription)
                        {
                            resolver = current;
                            return true;
                        }

                        resolver = null;
                    }
                    else
                    {
                        switch (current.Kind)
                        {
                            case ResolverKind.Batch:
                                resolver = current;

                                if (preference is PreferredResolverKind.Batch)
                                {
                                    return true;
                                }
                                break;

                            case ResolverKind.BatchByKey:
                                resolver = current;
                                break;

                            case ResolverKind.Query:
                            default:
                                if (preference is PreferredResolverKind.Query)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsNodeField(IObjectField field, IOperation operation)
        => operation.Type is OperationType.Query &&
            field.DeclaringType.Equals(operation.RootType) &&
            (field.Name.EqualsOrdinal("node") || field.Name.EqualsOrdinal("nodes"));

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

    private enum PreferredResolverKind
    {
        Query,
        Batch,
        Subscription
    }
}
