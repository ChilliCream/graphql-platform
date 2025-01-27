using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities;
using ThrowHelper = HotChocolate.Fusion.Utilities.ThrowHelper;

namespace HotChocolate.Fusion.Planning.Pipeline;

/// <summary>
/// The request planer will rewrite the <see cref="IOperation"/> into
/// execution steps that outline the rough structure of the execution
/// plan.
/// </summary>
/// <remarks>
/// Creates a new instance of <see cref="ExecutionStepDiscoveryMiddleware"/>.
/// </remarks>
/// <param name="schema">
/// The schema.
/// </param>
/// <param name="configuration">
/// The fusion gateway configuration.
/// </param>
/// <exception cref="ArgumentNullException">
/// <paramref name="schema"/> is <c>null</c> or <paramref name="configuration"/> is <c>null</c>.
/// </exception>
internal sealed class ExecutionStepDiscoveryMiddleware(
    ISchema schema,
    FusionGraphConfiguration configuration)
    : IQueryPlanMiddleware
{
    private readonly FusionGraphConfiguration _config = configuration
        ?? throw new ArgumentNullException(nameof(configuration));
    private readonly ISchema _schema = schema
        ?? throw new ArgumentNullException(nameof(schema));

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
        var selectionSetType = _config.GetType<ObjectTypeMetadata>(context.Operation.RootType.Name);
        var selections = context.Operation.RootSelectionSet.Selections;
        var backlog = new Queue<BacklogItem>();

        CreateExecutionSteps(
            context,
            backlog,
            selectionSetType,
            selections,
            null,
            null,
            false);

        while (backlog.TryDequeue(out var item))
        {
            CreateExecutionSteps(
                context,
                backlog,
                item.DeclaringTypeMetadata,
                item.Selections,
                item.ParentSelection,
                item.SelectionPath,
                item.PreferBatching);
        }

        next(context);
    }

    private void CreateExecutionSteps(
        QueryPlanContext context,
        Queue<BacklogItem> backlog,
        ObjectTypeMetadata selectionSetTypeMetadata,
        IReadOnlyList<ISelection> selections,
        ISelection? parentSelection,
        SelectionPath? parentSelectionPath,
        bool preferBatching)
    {
        var variablesInContext = new HashSet<string>();
        var operation = context.Operation;
        List<ISelection>? leftovers = null;
        var path = new List<ISelection>();

        // if this is the root selection set of a query we will
        // look for some special selections.
        if (!context.HasHandledSpecialQueryFields && parentSelection is null)
        {
            HandleSpecialQuerySelections(
                context,
                operation,
                ref selections,
                selectionSetTypeMetadata,
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
                selectionSetTypeMetadata);
            var executionStep = new SelectionExecutionStep(
                context.NextStepId(),
                subgraph,
                parentSelection,
                parentSelectionPath,
                _schema.GetType<IObjectType>(selectionSetTypeMetadata.Name),
                selectionSetTypeMetadata);
            leftovers = null;

            // we will first try to resolve and set an entity resolver.
            // The entity resolver is like a patch fetch from a subgraph where
            // the resulting data is merged into the current selection set.
            TrySetEntityResolver(
                selectionSetTypeMetadata,
                parentSelection,
                preferBatching,
                variablesInContext,
                subgraph,
                executionStep);

            foreach (var selection in current)
            {
                var pathIndex = path.Count;
                path.Add(selection);

                var field = selection.Field;
                var fieldInfo = selectionSetTypeMetadata.Fields[field.Name];

                if (!fieldInfo.Bindings.ContainsSubgraph(subgraph))
                {
                    (leftovers ??= []).Add(selection);
                    continue;
                }

                ResolverDefinition? resolver = null;

                GatherVariablesInContext(
                    selection,
                    selectionSetTypeMetadata,
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
                        selectionSetTypeMetadata,
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
                        parentSelectionPath,
                        path,
                        executionStep,
                        preferBatching,
                        context.ParentSelections);
                }

                path.RemoveAt(pathIndex);
            }

            // if the current execution step has now way to resolve the data
            // we will try to resolve it from the root.
            if(executionStep.ParentSelection is not null &&
                executionStep.ParentSelectionPath is not null &&
                executionStep.Resolver is null &&
                executionStep.SelectionResolvers.Count == 0)
            {
                if (!EnsureStepCanBeResolvedFromRoot(
                    executionStep.SubgraphName,
                    executionStep.ParentSelectionPath))
                {
                    throw ThrowHelper.NoResolverInContext();
                }
            }

            context.Steps.Add(executionStep);
        } while (leftovers is not null);
    }

    private void HandleSpecialQuerySelections(
        QueryPlanContext context,
        IOperation operation,
        ref IReadOnlyList<ISelection> selections,
        ObjectTypeMetadata selectionSetTypeMetadata,
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
                        selectionSetTypeMetadata);
                    (processed ??= []).Add(i);
                    continue;
                }

                if (IsNodeField(field, operation))
                {
                    AddNodeExecutionStep(
                        context,
                        backlog,
                        selection,
                        selectionSetTypeMetadata,
                        context.ParentSelections);
                    (processed ??= []).Add(i);
                }
            }

            if (processed is { Count: > 0, })
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
        SelectionPath? rootSelectionPath,
        List<ISelection> path,
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
                rootSelectionPath,
                path,
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
        SelectionPath? rootSelectionPath,
        List<ISelection> path,
        SelectionExecutionStep executionStep,
        IObjectType possibleType,
        bool preferBatching,
        Dictionary<ISelection, ISelection> parentSelectionLookup)
    {
        var declaringType = _config.GetType<ObjectTypeMetadata>(possibleType.Name);
        var selectionSet = operation.GetSelectionSet(parentSelection, possibleType);
        List<ISelection>? leftovers = null;

        executionStep.AllSelectionSets.Add(selectionSet);

        foreach (var selection in selectionSet.Selections)
        {
            var pathIndex = path.Count;
            path.Add(selection);

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
                        rootSelectionPath,
                        path,
                        executionStep,
                        preferBatching,
                        parentSelectionLookup);
                }
            }
            else
            {
                (leftovers ??= []).Add(selection);
            }

            path.RemoveAt(pathIndex);
        }

        if (leftovers is not null)
        {
            backlog.Enqueue(
                new BacklogItem(
                    parentSelection,
                    CreateSelectionPath(rootSelectionPath, path),
                    declaringType,
                    leftovers,
                    preferBatching));
        }
    }

    private static SelectionPath? CreateSelectionPath(SelectionPath? rootPath, List<ISelection> pathSegments)
    {
        var parent = rootPath;

        for (var i = 0; i < pathSegments.Count; i++)
        {
            parent = parent is null
                ? new SelectionPath(pathSegments[i])
                : parent.Append(pathSegments[i]);
        }

        return parent;
    }

    private static void AddIntrospectionStepIfNotExists(
        QueryPlanContext context,
        IObjectField field,
        IObjectType queryType,
        ObjectTypeMetadata queryTypeMetadata)
    {
        if (!context.HasIntrospectionSelections &&
            (field.Name.EqualsOrdinal(IntrospectionFields.Schema) ||
                field.Name.EqualsOrdinal(IntrospectionFields.Type) ||
                field.Name.EqualsOrdinal(IntrospectionFields.TypeName)))
        {
            var step = new IntrospectionExecutionStep(
                context.NextStepId(),
                queryType,
                queryTypeMetadata);

            context.Steps.Add(step);
            context.HasIntrospectionSelections = true;
        }
    }

    private void AddNodeExecutionStep(
        QueryPlanContext context,
        Queue<BacklogItem> backlog,
        ISelection nodeSelection,
        ObjectTypeMetadata queryTypeMetadata,
        Dictionary<ISelection, ISelection> parentSelectionLookup)
    {
        var operation = context.Operation;
        var queryType = operation.RootType;
        var nodeExecutionStep = new NodeExecutionStep(
            context.NextStepId(),
            nodeSelection,
            queryType,
            queryTypeMetadata);
        context.Steps.Add(nodeExecutionStep);

        foreach (var entityType in operation.GetPossibleTypes(nodeSelection))
        {
            var entityTypeInfo = _config.GetType<ObjectTypeMetadata>(entityType.Name);
            var selectionSet = operation.GetSelectionSet(nodeSelection, entityType);

            var selectionExecutionStep =
                CreateNodeNestedExecutionSteps(
                    context,
                    backlog,
                    nodeSelection,
                    queryTypeMetadata,
                    entityType,
                    entityTypeInfo,
                    selectionSet,
                    preferBatching: false,
                    parentSelectionLookup: parentSelectionLookup);

            var nodeEntityExecutionStep =
                new NodeEntityExecutionStep(
                    context.NextStepId(),
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
        ObjectTypeMetadata queryTypeMetadata,
        IObjectType entityType,
        ObjectTypeMetadata entityTypeMetadata,
        ISelectionSet entityTypeSelectionSet,
        bool preferBatching,
        Dictionary<ISelection, ISelection> parentSelectionLookup)
    {
        var variablesInContext = new HashSet<string>();
        var operation = context.Operation;
        var queryType = operation.RootType;

        // we will first determine from which subgraph we can
        // fetch an entity through the node resolver.
        var availableSubgraphs = _config.GetAvailableSubgraphs(entityTypeMetadata.Name);

        // next we determine the best subgraph to fetch from.
        var subgraph =
            availableSubgraphs.Count == 1
                ? availableSubgraphs[0]
                : _config.GetBestMatchingSubgraph(
                    operation,
                    entityTypeSelectionSet.Selections,
                    entityTypeMetadata,
                    availableSubgraphs);

        var field = nodeSelection.Field;
        var fieldInfo = queryTypeMetadata.Fields[field.Name];
        var executionStep = new SelectionExecutionStep(
            context.NextStepId(),
            subgraph, queryType,
            queryTypeMetadata);

        var preference = ChoosePreferredResolverKind(operation, null, preferBatching);
        GatherVariablesInContext(nodeSelection, queryTypeMetadata, null, variablesInContext);

        if (!TryGetResolver(fieldInfo, subgraph, variablesInContext, preference, out var resolver))
        {
            throw ThrowHelper.NoResolverInContext();
        }
        executionStep.AllSelections.Add(nodeSelection);
        executionStep.RootSelections.Add(new RootSelection(nodeSelection, resolver));

        DetermineRequiredVariables(
            nodeSelection,
            queryTypeMetadata,
            null,
            resolver,
            executionStep.Requires);

        CollectNestedSelections(
            backlog,
            operation,
            nodeSelection,
            null,
            [],
            executionStep,
            entityType,
            preferBatching,
            parentSelectionLookup);

        context.Steps.Add(executionStep);

        return executionStep;
    }

    private void TrySetEntityResolver(
        ObjectTypeMetadata selectionSetTypeMetadata,
        ISelection? parentSelection,
        bool preferBatching,
        HashSet<string> variablesInContext,
        string subgraph,
        SelectionExecutionStep executionStep)
    {
        if (parentSelection is null || !selectionSetTypeMetadata.Resolvers.ContainsResolvers(subgraph))
        {
            return;
        }

        GatherVariablesInContext(selectionSetTypeMetadata, parentSelection, variablesInContext);

        if (TryGetResolver(
            selectionSetTypeMetadata,
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
        ObjectTypeMetadata declaringTypeMetadata,
        string schemaName,
        HashSet<string> variablesInContext,
        bool preferBatching,
        [NotNullWhen(true)] out ResolverDefinition? resolver)
        => TryGetResolver(
            declaringTypeMetadata.Resolvers,
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
                        if (current.Kind is ResolverKind.Subscribe)
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
                                break;

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
        ObjectTypeMetadata declaringTypeMetadata,
        ISelection? parent,
        HashSet<string> variablesInContext)
    {
        variablesInContext.Clear();

        if (parent is not null)
        {
            var parentDeclaringType = _config.GetType<ObjectTypeMetadata>(parent.DeclaringType.Name);
            var parentField = parentDeclaringType.Fields[parent.Field.Name];

            foreach (var variable in parentField.Variables)
            {
                variablesInContext.Add(variable.Name);
            }
        }

        foreach (var variable in declaringTypeMetadata.Variables)
        {
            variablesInContext.Add(variable.Name);
        }

        var field = declaringTypeMetadata.Fields[selection.Field.Name];

        foreach (var variable in field.Variables)
        {
            variablesInContext.Add(variable.Name);
        }
    }

    private void GatherVariablesInContext(
        ObjectTypeMetadata declaringTypeMetadata,
        ISelection parent,
        HashSet<string> variablesInContext)
    {
        variablesInContext.Clear();

        var parentDeclaringType = _config.GetType<ObjectTypeMetadata>(parent.DeclaringType.Name);
        var parentField = parentDeclaringType.Fields[parent.Field.Name];

        foreach (var variable in parentField.Variables)
        {
            variablesInContext.Add(variable.Name);
        }

        foreach (var variable in declaringTypeMetadata.Variables)
        {
            variablesInContext.Add(variable.Name);
        }
    }

    private void DetermineRequiredVariables(
        ISelection selection,
        ObjectTypeMetadata declaringTypeMetadata,
        ISelection? parent,
        ResolverDefinition resolver,
        HashSet<string> requirements)
    {
        var field = declaringTypeMetadata.Fields[selection.Field.Name];
        var inContext = field.Variables.OfType<ArgumentVariableDefinition>().Select(t => t.Name);

        if (parent is not null)
        {
            var parentDeclaringType = _config.GetType<ObjectTypeMetadata>(parent.DeclaringType.Name);
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
        var parentDeclaringType = _config.GetType<ObjectTypeMetadata>(parent.DeclaringType.Name);
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

    private bool EnsureStepCanBeResolvedFromRoot(
        string subgraphName,
        SelectionPath path)
    {
        var current = path;

        while (current is not null)
        {
            var typeMetadata = _config.GetType<ObjectTypeMetadata>(current.Selection.DeclaringType.Name);

            if (!typeMetadata.Fields[current.Selection.Field.Name].Bindings.ContainsSubgraph(subgraphName))
            {
                return false;
            }

            current = current.Parent;
        }

        return true;
    }

    private readonly struct BacklogItem(
        ISelection parentSelection,
        SelectionPath? selectionPath,
        ObjectTypeMetadata declaringTypeMetadata,
        IReadOnlyList<ISelection> selections,
        bool preferBatching)
    {
        public ISelection ParentSelection { get; } = parentSelection;

        public SelectionPath? SelectionPath { get; } = selectionPath;

        public ObjectTypeMetadata DeclaringTypeMetadata { get; } = declaringTypeMetadata;

        public IReadOnlyList<ISelection> Selections { get; } = selections;

        public bool PreferBatching { get; } = preferBatching;
    }

    private enum PreferredResolverKind
    {
        Query,
        Batch,
        Subscription,
    }
}
