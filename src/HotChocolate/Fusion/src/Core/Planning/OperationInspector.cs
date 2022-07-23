using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;

namespace HotChocolate.Fusion.Planning;

// TODO : Name is incorrect
internal sealed class OperationInspector
{
    private readonly Metadata.Schema _schema;
    private readonly Queue<BacklogItem> _backlog = new();
    private readonly List<QueryPlanWorkItem> _workItems = new();

    public OperationInspector(Metadata.Schema schema)
    {
        _schema = schema;
    }

    public IReadOnlyList<QueryPlanWorkItem> Inspect(IOperation operation)
    {
        var declaringType = _schema.GetType<ObjectType>(operation.RootType.Name);
        var selections = operation.RootSelectionSet.Selections;

        // inspect operation
        Inspect(operation, declaringType, selections, null);

        while (_backlog.TryDequeue(out var item))
        {
            Inspect(operation, item.DeclaringType, item.Selections, item.ParentSelection);
        }

        // reorder for execution.


        return _workItems;
    }

    private void Inspect(
        IOperation operation,
        ObjectType declaringType,
        IReadOnlyList<ISelection> selections,
        ISelection? parentSelection)
    {
        var variablesInContext = new HashSet<string>();
        List<ISelection>? leftovers = null;

        do
        {
            var current = (IReadOnlyList<ISelection>?)leftovers ?? selections;
            var schemaName = ResolveBestMatchingSchema(operation, current, declaringType);
            var workItem = new QueryPlanWorkItem(schemaName, declaringType, parentSelection);
            _workItems.Add(workItem);
            leftovers = null;
            FetchDefinition? resolver;

            if (parentSelection is not null &&
                declaringType.Resolvers.ContainsResolvers(schemaName))
            {
                CalculateVariablesInContext(declaringType, parentSelection, variablesInContext);
                if (TryGetResolver(declaringType, schemaName, variablesInContext, out resolver))
                {
                    workItem.Resolver = resolver;
                    CalculateRequirements(parentSelection, resolver, workItem.Requires);
                }
            }

            foreach (var selection in current)
            {
                var field = declaringType.Fields[selection.Field.Name];
                if (field.Bindings.TryGetValue(schemaName, out _))
                {
                    CalculateVariablesInContext(
                        selection,
                        declaringType,
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
                            declaringType,
                            parentSelection,
                            resolver,
                            workItem.Requires);
                    }

                    workItem.AllSelections.Add(selection);
                    workItem.RootSelections.Add(new RootSelection(selection, resolver));

                    if (selection.SelectionSet is not null)
                    {
                        CollectChildSelections(operation, selection, workItem);
                    }
                }
                else
                {
                    (leftovers ??= new()).Add(selection);
                }
            }
        } while (leftovers is not null);
    }

    private void CollectChildSelections(
        IOperation operation,
        ISelection parentSelection,
        QueryPlanWorkItem workItem)
    {
        foreach (var possibleType in operation.GetPossibleTypes(parentSelection))
        {
            var declaringType = _schema.GetType<ObjectType>(possibleType.Name);
            var selectionSet = operation.GetSelectionSet(parentSelection, possibleType);
            List<ISelection>? leftovers = null;

            foreach (var selection in selectionSet.Selections)
            {
                var field = declaringType.Fields[selection.Field.Name];

                if (field.Bindings.TryGetValue(workItem.SchemaName, out _))
                {
                    workItem.AllSelections.Add(selection);
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
        var bestSchema = _schema.Bindings[0];

        foreach (var schemaName in _schema.Bindings)
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
            if (typeContext.Fields[selection.Field.Name].Bindings.ContainsSchema(schemaName))
            {
                score++;

                if (selection.SelectionSet is not null)
                {
                    foreach (var possibleType in operation.GetPossibleTypes(selection))
                    {
                        var type = _schema.GetType<ObjectType>(possibleType.Name);
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

    private bool TryGetResolver(
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

    private bool TryGetResolver(
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
            var parentDeclaringType = _schema.GetType<ObjectType>(parent.DeclaringType.Name);
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

        var parentDeclaringType = _schema.GetType<ObjectType>(parent.DeclaringType.Name);
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
            var parentDeclaringType = _schema.GetType<ObjectType>(parent.DeclaringType.Name);
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
        var parentDeclaringType = _schema.GetType<ObjectType>(parent.DeclaringType.Name);
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

// The name of this class is wrong.
internal class QueryPlanWorkItem : IExecutionStep
{
    public QueryPlanWorkItem(
        string schemaNameName,
        ObjectType declaringType,
        ISelection? parentSelection)
    {
        DeclaringType = declaringType;
        ParentSelection = parentSelection;
        SchemaName = schemaNameName;
    }

    public string SchemaName { get; }

    public ObjectType DeclaringType { get; }

    public ISelection? ParentSelection { get; }

    public FetchDefinition? Resolver { get; set; }

    public List<RootSelection> RootSelections { get; } = new();

    public HashSet<ISelection> AllSelections { get; } = new();

    public List<QueryPlanWorkItem> DependsOn { get; } = new();

    public List<ExportDefinition> Exports { get; } = new();

    public HashSet<string> Requires { get; } = new();
}

internal readonly struct ExportDefinition
{
    public ExportDefinition(string name, FieldVariableDefinition variable)
    {
        Name = name;
        Variable = variable;
    }

    public string Name { get; }

    public FieldVariableDefinition Variable { get; }
}

internal readonly struct RootSelection
{
    public RootSelection(ISelection selection, FetchDefinition? resolver)
    {
        Selection = selection;
        Resolver = resolver;
    }

    public ISelection Selection { get; }

    public FetchDefinition? Resolver { get; }
}

