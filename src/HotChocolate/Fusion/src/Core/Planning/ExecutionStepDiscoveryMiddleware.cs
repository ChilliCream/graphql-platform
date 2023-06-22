using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// The request planer will rewrite the <see cref="IOperation"/> into
/// execution steps that outline the rough structure of the execution
/// plan.
/// </summary>
internal sealed class ExecutionStepDiscoveryMiddleware : IQueryPlanMiddleware
{
    private readonly FusionGraphConfiguration _config;
    private readonly ISchema _schema;

    /// <summary>
    /// Creates a new instance of <see cref="ExecutionStepDiscoveryMiddleware"/>.
    /// </summary>
    /// <param name="schema">
    /// The schema.
    /// </param>
    /// <param name="configuration">
    /// The fusion gateway configuration.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="schema"/> is <c>null</c> or <paramref name="configuration"/> is <c>null</c>.
    /// </exception>
    public ExecutionStepDiscoveryMiddleware(ISchema schema, FusionGraphConfiguration configuration)
    {
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        _config = configuration ?? throw new ArgumentNullException(nameof(configuration));
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
        var selectionSetType = _config.GetType<ObjectTypeInfo>(context.Operation.RootType.Name);
        var selections = context.Operation.RootSelectionSet.Selections;
        var backlog = new Queue<BacklogItem>();

        CreateExecutionSteps(
            context,
            backlog,
            selectionSetType,
            selections,
            null,
            false);

        while (backlog.TryDequeue(out var item))
        {
            CreateExecutionSteps(
                context,
                backlog,
                item.DeclaringTypeInfo,
                item.Selections,
                item.ParentSelection,
                item.PreferBatching);
        }

        next(context);
    }

    private void CreateExecutionSteps(
        QueryPlanContext context,
        Queue<BacklogItem> backlog,
        ObjectTypeInfo selectionSetTypeInfo,
        IReadOnlyList<ISelection> selections,
        ISelection? parentSelection,
        bool preferBatching)
    {
        var variablesInContext = new HashSet<string>();
        var operation = context.Operation;
        List<ISelection>? leftovers = null;

        // if this is the root selection set of a query we will
        // look for some special selections.
        if (!context.HasHandledSpecialQueryFields && parentSelection is null)
        {
            HandleSpecialQuerySelections(
                context,
                operation,
                ref selections,
                selectionSetTypeInfo,
                backlog);
            context.HasHandledSpecialQueryFields = true;

            if (selections.Count == 0)
            {
                return;
            }
        }

        do
        {
            var current = leftovers ?? selections;
            var subgraph = _config.GetBestMatchingSubgraph(
                operation,
                current,
                selectionSetTypeInfo);
            var executionStep = new SelectionExecutionStep(
                subgraph,
                parentSelection,
                _schema.GetType<IObjectType>(selectionSetTypeInfo.Name),
                selectionSetTypeInfo);
            leftovers = null;

            // we will first try to resolve and set an entity resolver.
            // The entity resolver is like a patch fetch from a subgraph where
            // the resulting data is merged into the current selection set.
            TrySetEntityResolver(
                selectionSetTypeInfo,
                parentSelection,
                preferBatching,
                variablesInContext,
                subgraph,
                executionStep);

            foreach (var selection in current)
            {
                var field = selection.Field;
                var fieldInfo = selectionSetTypeInfo.Fields[field.Name];

                if (!fieldInfo.Bindings.ContainsSubgraph(subgraph))
                {
                    (leftovers ??= new()).Add(selection);
                    continue;
                }

                ResolverDefinition? resolver = null;

                GatherVariablesInContext(
                    selection,
                    selectionSetTypeInfo,
                    parentSelection,
                    variablesInContext);

                if (fieldInfo.Resolvers.ContainsResolvers(subgraph))
                {
                    var resolverPreference =
                        ChoosePreferredResolverKind(
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

                    DetermineRequiredVariables(
                        selection,
                        selectionSetTypeInfo,
                        parentSelection,
                        resolver,
                        executionStep.Requires);
                }

                executionStep.AllSelections.Add(selection);
                executionStep.RootSelections.Add(new RootSelection(selection, resolver));

                if (resolver is not null)
                {
                    executionStep.SelectionResolvers.Add(selection, resolver);
                }

                if (selection.SelectionSet is not null)
                {
                    CollectNestedSelections(
                        backlog,
                        operation,
                        selection,
                        executionStep,
                        preferBatching,
                        context.ParentSelections);
                }
            }

            context.Steps.Add(executionStep);
        } while (leftovers is not null);
    }

    private void HandleSpecialQuerySelections(
        QueryPlanContext context,
        IOperation operation,
        ref IReadOnlyList<ISelection> selections,
        ObjectTypeInfo selectionSetTypeInfo,
        Queue<BacklogItem> backlog)
    {
        if (operation.Type is OperationType.Query)
        {
            List<int>? processed = null;

            for (var i = 0; i < selections.Count; i++)
            {
                var selection = selections[i];
                var field = selection.Field;

                if (field.IsIntrospectionField)
                {
                    AddIntrospectionStepIfNotExists(
                        context,
                        field,
                        operation.RootType,
                        selectionSetTypeInfo);
                    (processed ??= new()).Add(i);
                    continue;
                }

                if (IsNodeField(field, operation))
                {
                    AddNodeExecutionStep(
                        context,
                        backlog,
                        selection,
                        selectionSetTypeInfo,
                        context.ParentSelections);
                    (processed ??= new()).Add(i);
                }
            }

            if (processed is { Count: > 0 })
            {
                var temp = selections.ToList();

                for (var i = processed.Count - 1; i >= 0; i--)
                {
                    temp.RemoveAt(processed[i]);
                }

                selections = temp;
            }
        }
    }

    private void CollectNestedSelections(
        Queue<BacklogItem> backlog,
        IOperation operation,
        ISelection parentSelection,
        SelectionExecutionStep executionStep,
        bool preferBatching,
        Dictionary<ISelection, ISelection> parentSelectionLookup)
    {
        if (!preferBatching)
        {
            preferBatching = parentSelection.Type.IsListType();
        }

        foreach (var possibleType in operation.GetPossibleTypes(parentSelection))
        {
            CollectNestedSelections(
                backlog,
                operation,
                parentSelection,
                executionStep,
                possibleType,
                preferBatching,
                parentSelectionLookup);
        }
    }

    private void CollectNestedSelections(
        Queue<BacklogItem> backlog,
        IOperation operation,
        ISelection parentSelection,
        SelectionExecutionStep executionStep,
        IObjectType possibleType,
        bool preferBatching,
        Dictionary<ISelection, ISelection> parentSelectionLookup)
    {
        var declaringType = _config.GetType<ObjectTypeInfo>(possibleType.Name);
        var selectionSet = operation.GetSelectionSet(parentSelection, possibleType);
        List<ISelection>? leftovers = null;

        executionStep.AllSelectionSets.Add(selectionSet);

        foreach (var selection in selectionSet.Selections)
        {
            parentSelectionLookup.TryAdd(selection, parentSelection);
            var field = declaringType.Fields[selection.Field.Name];

            if (field.Bindings.TryGetBinding(executionStep.SubgraphName, out _))
            {
                executionStep.AllSelections.Add(selection);

                if (field.Resolvers.ContainsResolvers(executionStep.SubgraphName))
                {
                    var variablesInContext = new HashSet<string>();

                    GatherVariablesInContext(
                        selection,
                        declaringType,
                        parentSelection,
                        variablesInContext);

                    var resolverPreference =
                        ChoosePreferredResolverKind(
                            operation,
                            parentSelection,
                            preferBatching);

                    if (!TryGetResolver(
                        field,
                        executionStep.SubgraphName,
                        variablesInContext,
                        resolverPreference,
                        out var resolver))
                    {
                        throw ThrowHelper.NoResolverInContext();
                    }

                    DetermineRequiredVariables(
                        selection,
                        declaringType,
                        parentSelection,
                        resolver,
                        executionStep.Requires);

                    executionStep.SelectionResolvers.Add(selection, resolver);
                }

                if (selection.SelectionSet is not null)
                {
                    CollectNestedSelections(
                        backlog,
                        operation,
                        selection,
                        executionStep,
                        preferBatching,
                        parentSelectionLookup);
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

    private static void AddIntrospectionStepIfNotExists(
        QueryPlanContext context,
        IObjectField field,
        IObjectType queryType,
        ObjectTypeInfo queryTypeInfo)
    {
        if (!context.HasIntrospectionSelections &&
            (field.Name.EqualsOrdinal(IntrospectionFields.Schema) ||
                field.Name.EqualsOrdinal(IntrospectionFields.Type) ||
                field.Name.EqualsOrdinal(IntrospectionFields.TypeName)))
        {
            context.Steps.Add(new IntrospectionExecutionStep(queryType, queryTypeInfo));
            context.HasIntrospectionSelections = true;
        }
    }

    private void AddNodeExecutionStep(
        QueryPlanContext context,
        Queue<BacklogItem> backlog,
        ISelection nodeSelection,
        ObjectTypeInfo queryTypeInfo,
        Dictionary<ISelection, ISelection> parentSelectionLookup)
    {
        var operation = context.Operation;
        var queryType = operation.RootType;
        var nodeExecutionStep = new NodeExecutionStep(nodeSelection, queryType, queryTypeInfo);
        context.Steps.Add(nodeExecutionStep);

        foreach (var entityType in operation.GetPossibleTypes(nodeSelection))
        {
            var entityTypeInfo = _config.GetType<ObjectTypeInfo>(entityType.Name);
            var selectionSet = operation.GetSelectionSet(nodeSelection, entityType);

            var selectionExecutionStep =
                CreateNodeNestedExecutionSteps(
                    context,
                    backlog,
                    nodeSelection,
                    queryTypeInfo,
                    entityType,
                    entityTypeInfo,
                    selectionSet,
                    preferBatching: false,
                    parentSelectionLookup: parentSelectionLookup);

            var nodeEntityExecutionStep =
                new NodeEntityExecutionStep(
                    entityType,
                    entityTypeInfo,
                    selectionExecutionStep);

            // the dependsOn relationship is used to build the execution tree.
            // since the nodeEntityExecutionStep is removed later in the pipeline we need
            // a direct dependency between selection and node.
            selectionExecutionStep.DependsOn.Add(nodeExecutionStep);
            nodeEntityExecutionStep.DependsOn.Add(nodeExecutionStep);
            nodeExecutionStep.EntitySteps.Add(nodeEntityExecutionStep);

            context.Steps.Add(nodeEntityExecutionStep);
        }
    }

    private SelectionExecutionStep CreateNodeNestedExecutionSteps(
        QueryPlanContext context,
        Queue<BacklogItem> backlog,
        ISelection nodeSelection,
        ObjectTypeInfo queryTypeInfo,
        IObjectType entityType,
        ObjectTypeInfo entityTypeInfo,
        ISelectionSet entityTypeSelectionSet,
        bool preferBatching,
        Dictionary<ISelection, ISelection> parentSelectionLookup)
    {
        var variablesInContext = new HashSet<string>();
        var operation = context.Operation;
        var queryType = operation.RootType;

        // we will first determine from which subgraph we can
        // fetch an entity through the node resolver.
        var availableSubgraphs = _config.GetAvailableSubgraphs(entityTypeInfo.Name);

        // next we determine the best subgraph to fetch from.
        var subgraph =
            availableSubgraphs.Count == 1
                ? availableSubgraphs[0]
                : _config.GetBestMatchingSubgraph(
                    operation,
                    entityTypeSelectionSet.Selections,
                    entityTypeInfo,
                    availableSubgraphs);


        var field = nodeSelection.Field;
        var fieldInfo = queryTypeInfo.Fields[field.Name];
        var executionStep = new SelectionExecutionStep(subgraph, queryType,  queryTypeInfo);

        var preference = ChoosePreferredResolverKind(operation, null, preferBatching);
        GatherVariablesInContext(nodeSelection, queryTypeInfo, null, variablesInContext);

        if (!TryGetResolver(fieldInfo, subgraph, variablesInContext, preference, out var resolver))
        {
            throw ThrowHelper.NoResolverInContext();
        }
        executionStep.AllSelections.Add(nodeSelection);
        executionStep.RootSelections.Add(new RootSelection(nodeSelection, resolver));

        DetermineRequiredVariables(
            nodeSelection,
            queryTypeInfo,
            null,
            resolver,
            executionStep.Requires);

        CollectNestedSelections(
            backlog,
            operation,
            nodeSelection,
            executionStep,
            entityType,
            preferBatching,
            parentSelectionLookup);

        context.Steps.Add(executionStep);

        return executionStep;
    }

    private void TrySetEntityResolver(
        ObjectTypeInfo selectionSetTypeInfo,
        ISelection? parentSelection,
        bool preferBatching,
        HashSet<string> variablesInContext,
        string subgraph,
        SelectionExecutionStep executionStep)
    {
        if (parentSelection is null || !selectionSetTypeInfo.Resolvers.ContainsResolvers(subgraph))
        {
            return;
        }

        GatherVariablesInContext(selectionSetTypeInfo, parentSelection, variablesInContext);

        if (TryGetResolver(
            selectionSetTypeInfo,
            subgraph,
            variablesInContext,
            preferBatching,
            out var resolver))
        {
            executionStep.Resolver = resolver;
            DetermineRequiredVariables(parentSelection, resolver, executionStep.Requires);
        }
    }

    private static PreferredResolverKind ChoosePreferredResolverKind(
        IOperation operation,
        ISelection? parentSelection,
        bool preferBatching)
        => operation.Type is OperationType.Subscription &&
            parentSelection is null
                ? PreferredResolverKind.Subscription
                : preferBatching
                    ? PreferredResolverKind.Batch
                    : PreferredResolverKind.Query;


    private static bool TryGetResolver(
        ObjectFieldInfo fieldInfo,
        string schemaName,
        HashSet<string> variablesInContext,
        PreferredResolverKind preference,
        [NotNullWhen(true)] out ResolverDefinition? resolver)
        => TryGetResolver(
            fieldInfo.Resolvers,
            schemaName,
            variablesInContext,
            preference,
            out resolver);

    private static bool TryGetResolver(
        ObjectTypeInfo declaringTypeInfo,
        string schemaName,
        HashSet<string> variablesInContext,
        bool preferBatching,
        [NotNullWhen(true)] out ResolverDefinition? resolver)
        => TryGetResolver(
            declaringTypeInfo.Resolvers,
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

    private void GatherVariablesInContext(
        ISelection selection,
        ObjectTypeInfo declaringTypeInfo,
        ISelection? parent,
        HashSet<string> variablesInContext)
    {
        variablesInContext.Clear();

        if (parent is not null)
        {
            var parentDeclaringType = _config.GetType<ObjectTypeInfo>(parent.DeclaringType.Name);
            var parentField = parentDeclaringType.Fields[parent.Field.Name];

            foreach (var variable in parentField.Variables)
            {
                variablesInContext.Add(variable.Name);
            }
        }

        foreach (var variable in declaringTypeInfo.Variables)
        {
            variablesInContext.Add(variable.Name);
        }

        var field = declaringTypeInfo.Fields[selection.Field.Name];

        foreach (var variable in field.Variables)
        {
            variablesInContext.Add(variable.Name);
        }
    }

    private void GatherVariablesInContext(
        ObjectTypeInfo declaringTypeInfo,
        ISelection parent,
        HashSet<string> variablesInContext)
    {
        variablesInContext.Clear();

        var parentDeclaringType = _config.GetType<ObjectTypeInfo>(parent.DeclaringType.Name);
        var parentField = parentDeclaringType.Fields[parent.Field.Name];

        foreach (var variable in parentField.Variables)
        {
            variablesInContext.Add(variable.Name);
        }

        foreach (var variable in declaringTypeInfo.Variables)
        {
            variablesInContext.Add(variable.Name);
        }
    }

    private void DetermineRequiredVariables(
        ISelection selection,
        ObjectTypeInfo declaringTypeInfo,
        ISelection? parent,
        ResolverDefinition resolver,
        HashSet<string> requirements)
    {
        var field = declaringTypeInfo.Fields[selection.Field.Name];
        var inContext = field.Variables.OfType<ArgumentVariableDefinition>().Select(t => t.Name);

        if (parent is not null)
        {
            var parentDeclaringType = _config.GetType<ObjectTypeInfo>(parent.DeclaringType.Name);
            var parentField = parentDeclaringType.Fields[parent.Field.Name];
            inContext = inContext.Concat(parentField.Variables.Select(t => t.Name));
        }

        foreach (var requirement in resolver.Requires.Except(inContext))
        {
            requirements.Add(requirement);
        }
    }

    private void DetermineRequiredVariables(
        ISelection parent,
        ResolverDefinition resolver,
        HashSet<string> requirements)
    {
        var parentDeclaringType = _config.GetType<ObjectTypeInfo>(parent.DeclaringType.Name);
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
            ObjectTypeInfo declaringTypeInfo,
            IReadOnlyList<ISelection> selections,
            bool preferBatching)
        {
            ParentSelection = parentSelection;
            DeclaringTypeInfo = declaringTypeInfo;
            Selections = selections;
            PreferBatching = preferBatching;
        }

        public ISelection ParentSelection { get; }

        public ObjectTypeInfo DeclaringTypeInfo { get; }

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
