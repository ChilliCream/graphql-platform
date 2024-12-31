using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Planning.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

public sealed class OperationPlanner(CompositeSchema schema)
{
    private readonly MergeSelectionSetRewriter _selectionSetMergeRewriter = new(schema);
    private int _lastRequirementId;

    public RequestPlanNode CreatePlan(DocumentNode document, string? operationName)
    {
        ArgumentNullException.ThrowIfNull(document);

        var operationDefinition = document.GetOperation(operationName);
        var operationType = schema.GetOperationType(operationDefinition.Operation);
        var schemasWeighted = GetSchemasWeighted(operationType, operationDefinition.SelectionSet);
        var operationPlan = new RequestPlanNode(document, operationName);

        // TODO: this need to be rewritten to check if everything is planned for.
        // At the moment this is just our staging area.
        foreach (var schemaName in schemasWeighted.OrderByDescending(t => t.Value).Select(t => t.Key))
        {
            var operation = new OperationPlanNode(
                schemaName,
                operationType,
                operationDefinition.SelectionSet);

            var context = new PlaningContext(operation);

            PlanSelectionSet(context, operationType);

            if (operation.UnresolvableSelections.Any())
            {
                throw new InvalidOperationException(
                    "We have to rework this method.");
            }

            TryMakeOperationConditional(operation, operation.Selections);
            operationPlan.AddOperation(operation);
        }

        OperationVariableBinder.BindOperationVariables(operationDefinition, operationPlan);

        return operationPlan;
    }

    private bool TryPlanSelectionSet(PlaningContext context)
    {
        var type = (CompositeComplexType)context.Parent.DeclaringType;

        PlanSelectionSet(context, type);

        if (context.Parent.UnresolvableSelections.Count == 0)
        {
            return true;
        }

        if (!HasLookup(type))
        {
            return context.Parent.TryMoveRequirementsToParent();
        }

        return TryHandleUnresolvedSelections(context, type);
    }

    private void PlanSelectionSet(PlaningContext context, CompositeComplexType type)
    {
        if (context.Parent.SelectionNodes is null)
        {
            throw new InvalidOperationException(
                "A leaf field cannot be a parent node.");
        }

        var haveConditionalSelectionsBeenRemoved = false;

        foreach (var selection in context.Parent.SelectionNodes)
        {
            if (IsSelectionAlwaysSkipped(selection))
            {
                haveConditionalSelectionsBeenRemoved = true;
                continue;
            }

            TryPlanSelection(
                context,
                type,
                selection);
        }

        if (haveConditionalSelectionsBeenRemoved)
        {
            // If we have removed conditional selections from a composite field, we need to add a __typename field
            // to have a valid selection set.
            if (context.Parent is FieldPlanNode { Selections.Count: 0 } fieldPlanNode)
            {
                // TODO: How to properly create a __typename field?
                var outputFieldInfo = new OutputFieldInfo("__typename", schema.GetType("String"), []);
                fieldPlanNode.AddSelection(new FieldPlanNode(new FieldNode("__typename"), outputFieldInfo));
            }

            // TODO : I have removed the operation validation ... when an op endsup without selections... we need to do
            // this in a different place.
        }
    }

    private bool TryPlanSelection(
        PlaningContext context,
        CompositeComplexType type,
        ISelectionNode selectionNode)
    {
        if (selectionNode is FieldNode fieldNode)
        {
            return TryPlanFieldSelection(
                context,
                type,
                fieldNode);
        }

        if (selectionNode is InlineFragmentNode inlineFragmentNode)
        {
            return TryPlanInlineFragmentSelection(
                context,
                type,
                inlineFragmentNode);
        }

        return false;
    }

    private bool TryPlanInlineFragmentSelection(
        PlaningContext context,
        CompositeComplexType type,
        InlineFragmentNode inlineFragmentNode)
    {
        var typeCondition = type;
        if (inlineFragmentNode.TypeCondition?.Name.Value is { } typeConditionName
            // TODO: CompositeComplexType does not include unions which are a valid value for type conditions.
            && schema.TryGetType<CompositeComplexType>(typeConditionName, out var typeConditionType))
        {
            typeCondition = typeConditionType;
        }

        var inlineFragmentPlanNode = new InlineFragmentPlanNode(typeCondition, inlineFragmentNode);
        var inlineFragmentContext = context with
        {
            Parent = inlineFragmentPlanNode, Path = context.Path.Push(inlineFragmentPlanNode)
        };

        foreach (var selection in inlineFragmentNode.SelectionSet.Selections)
        {
            if (IsSelectionAlwaysSkipped(selection))
            {
                continue;
            }

            TryPlanSelection(
                inlineFragmentContext,
                typeCondition,
                selection);
        }

        if (inlineFragmentPlanNode.UnresolvableSelections.Count > 0)
        {
            inlineFragmentPlanNode.TryMoveRequirementsTo(context.Parent);
        }

        if (inlineFragmentPlanNode.Selections.Count > 0)
        {
            AddSelectionDirectives(inlineFragmentPlanNode, inlineFragmentNode.Directives);
            context.Parent.AddSelection(inlineFragmentPlanNode);
            return true;
        }

        return false;
    }

    private bool TryPlanFieldSelection(
        PlaningContext context,
        CompositeComplexType type,
        FieldNode fieldNode)
    {
        if (!type.Fields.TryGetField(fieldNode.Name.Value, out var field))
        {
            throw new InvalidOperationException(
                "There is an unknown field in the selection set.");
        }

        // if we have an operation plan node we have a pre-validated set of
        // root fields, so we now the field will be resolvable on the
        // source schema.
        if (context.Parent is OperationPlanNode
            || field.Sources.ContainsSchema(context.Operation.SchemaName))
        {
            var source = field.Sources[context.Operation.SchemaName];
            var fieldNamedType = field.Type.NamedType();

            if (source.Requirements is not null)
            {
                context.Parent.AddDataRequirement(source.Requirements.SelectionSet, context.Path);
            }

            // if the field has no selection set it must be a leaf type.
            // This also means that if this field is resolvable that we can
            // just include it and no further processing is required.
            if (fieldNode.SelectionSet is null)
            {
                if (fieldNamedType.Kind is not TypeKind.Enum and not TypeKind.Scalar)
                {
                    throw new InvalidOperationException(
                        "Only complex types can have a selection set.");
                }

                var leafField = new FieldPlanNode(fieldNode, field);
                AddSelectionDirectives(leafField, fieldNode.Directives);
                context.Parent.AddSelection(leafField);
                return true;
            }

            // if this field as a selection set it must be an object, interface or union type,
            // otherwise the validation should have caught this. So, we just throw here if this
            // is not the case.
            if (fieldNamedType.Kind != TypeKind.Object
                && fieldNamedType.Kind != TypeKind.Interface
                && fieldNamedType.Kind != TypeKind.Union)
            {
                throw new InvalidOperationException(
                    "Only object, interface, or union types can have a selection set.");
            }

            var fieldPlanNode = new FieldPlanNode(fieldNode, field);
            AddSelectionDirectives(fieldPlanNode, fieldNode.Directives);

            if (TryPlanSelectionSet(context with { Parent = fieldPlanNode, Path = context.Path.Push(fieldPlanNode) }))
            {
                context.Parent.AddSelection(fieldPlanNode);
                return true;
            }
        }

        context.Parent.AddUnresolvableSelection(fieldNode, context.Path);
        return false;
    }

    private void AddSelectionDirectives(
        SelectionPlanNode selection,
        IReadOnlyList<DirectiveNode> directiveNodes)
    {
        foreach (var directiveNode in directiveNodes)
        {
            var directiveType = schema.GetDirectiveType(directiveNode.Name.Value);

            if ((directiveType == schema.SkipDirective || directiveType == schema.IncludeDirective)
                && directiveNode.Arguments[0].Value is BooleanValueNode)
            {
                continue;
            }

            var argumentAssignments = directiveNode.Arguments.Select(
                a => new ArgumentAssignment(a.Name.Value, a.Value)).ToList();
            selection.AddDirective(new CompositeDirective(directiveType, argumentAssignments));
        }
    }

    private bool TryHandleUnresolvedSelections(
        PlaningContext context,
        CompositeComplexType type)
    {
        if (!TryResolveEntityType(context.Parent, out var entityPath))
        {
            return false;
        }

        var requirementsList = context.Parent.TakeDataRequirements();
        var requirements = _selectionSetMergeRewriter.RewriteSelectionSets(requirementsList, type);
        var schemasWeighted = GetSchemasWeighted(type, requirements);
        schemasWeighted.Remove(context.Operation.SchemaName);

        // if we have found an entity to branch of from we will check
        // if any of the unresolved selections can be resolved through one of the entity lookups.
        var schemasInContext = new Dictionary<string, OperationPlanNode>
        {
            { context.Operation.SchemaName, context.Operation }
        };

        foreach (var schemaName in schemasWeighted.OrderByDescending(t => t.Value).Select(t => t.Key))
        {
            if (schemasInContext.ContainsKey(schemaName))
            {
                continue;
            }

            // next we try to find a lookup
            if (!TryGetLookup(
                (SelectionPlanNode)entityPath.Peek(),
                schemaName,
                schemasInContext.Keys,
                out var lookup,
                out var fieldSchemaDependencies))
            {
                continue;
            }

            var (lookupOp, lookupField) = CreateLookupOperation(schemaName, lookup, type, context.Parent, requirements);
            PlanSelectionSet(new PlaningContext(lookupOp, lookupField), type);

            // TODO: this will not work always as requirements from subsequent runs might need to be resolved with
            // previous node.
            // we collect new requirements and unresolved selections.
            requirementsList = lookupField.TakeDataRequirements();
            requirements = _selectionSetMergeRewriter.RewriteSelectionSets(requirementsList, type);

            schemasInContext.Add(schemaName, lookupOp);
            TryMakeOperationConditional(lookupOp, lookupField.Selections);

            // we add the lookup operation to all the schemas that we have requirements with.
            foreach (var requiredSchema in fieldSchemaDependencies.Values.Distinct())
            {
                // Add child node is wrong ... this is a graph and the lookup operation has dependencies on
                // this operation. We should probably double link here.
                // maybe AddDependantNode()?
                schemasInContext[requiredSchema].AddDependantOperation(lookupOp);
            }

            // add requirements to the operation
            for (var i = 0; i < lookup.Fields.Length; i++)
            {
                var requirementName = GetNextRequirementName();
                var requiredField = lookup.Fields[i];
                var argument = lookup.Arguments[i];

                var requiredFromSchema = fieldSchemaDependencies[requiredField];
                var requiredFromOperation = schemasInContext[requiredFromSchema];
                var requiredFromSelectionSet = requiredFromOperation != context.Operation
                    ? requiredFromOperation.Selections.Single()
                    : context.Parent;
                var requiredFromPathStack = requiredFromOperation != context.Operation
                    ? ImmutableStack<SelectionPlanNode>.Empty.Push(lookupField)
                    : context.Path;
                var requiredFromPath = CreateFieldPath(requiredFromPathStack);
                var requiredFromContext = new PlaningContext(
                    requiredFromOperation,
                    requiredFromSelectionSet,
                    requiredFromPathStack);

                if (!TryPlanSelection(
                    requiredFromContext,
                    (CompositeComplexType)requiredFromSelectionSet.DeclaringType,
                    CreateFieldNodeFromPath(requiredField)))
                {
                    return false;
                }

                var requirement = new FieldRequirementPlanNode(
                    requirementName,
                    requiredFromOperation,
                    requiredFromPath,
                    requiredField,
                    argument.Type);

                lookupOp.AddRequirement(requirement);
                lookupField.AddArgument(new ArgumentAssignment(argument.Name, new VariableNode(requirementName)));
            }
        }

        return requirementsList.Count == 0;
    }



    /// <summary>
    /// Tries to find an entity type in the current selection path.
    /// </summary>
    private static bool TryResolveEntityType(
        SelectionPlanNode parent,
        [NotNullWhen(true)] out Stack<PlanNode>? entityPath)
    {
        var current = parent;
        entityPath = new Stack<PlanNode>();
        entityPath.Push(parent);

        // if the current SelectionPlanNode is not an entity we will move up the selection path.
        while (current is { IsEntity: false, Parent: SelectionPlanNode parentSelection })
        {
            current = parentSelection;
            entityPath.Push(current);
        }

        if (!current.IsEntity)
        {
            entityPath = null;
            return false;
        }

        return true;
    }

    private static bool TryGetLookup(
        SelectionPlanNode selection,
        string schemaName,
        IReadOnlyCollection<string> schemasInContext,
        [NotNullWhen(true)] out Lookup? lookup,
        [NotNullWhen(true)] out ImmutableDictionary<FieldPath, string>? fieldSchemaDependencies)
    {
        var declaringType = (CompositeComplexType)selection.DeclaringType;
        var builder = ImmutableDictionary.CreateBuilder<FieldPath, string>();

        if (declaringType.Sources.TryGetType(schemaName, out var source) && source.Lookups.Length > 0)
        {
            foreach (var possibleLookup in source.Lookups.OrderBy(t => t.Fields.Length))
            {
                builder.Clear();

                foreach (var field in possibleLookup.Fields)
                {
                    if (!IsResolvable(declaringType, field, schemasInContext, out var requiredSchema))
                    {
                        break;
                    }

                    builder.Add(field, requiredSchema);
                }

                lookup = possibleLookup;
                fieldSchemaDependencies = builder.ToImmutable();
                return true;
            }
        }

        lookup = null;
        fieldSchemaDependencies = null;
        return false;
    }

    private static bool IsResolvable(
        ICompositeType type,
        FieldPath fieldPath,
        IEnumerable<string> schemasInContext,
        [NotNullWhen(true)] out string? requiredSchema)
    {
        foreach (var schemaName in schemasInContext)
        {
            if (IsResolvable(type, fieldPath, schemaName))
            {
                requiredSchema = schemaName;
                return true;
            }
        }

        requiredSchema = null;
        return false;
    }

    private static bool IsResolvable(
        ICompositeType type,
        FieldPath fieldPath,
        string schemaName)
    {
        foreach (var segment in fieldPath.Reverse())
        {
            if (type.NamedType() is not CompositeComplexType complexType
                || !complexType.Fields.TryGetField(segment.Name, out var field)
                || !field.Sources.TryGetMember(schemaName, out var source)
                || source.Requirements is not null)
            {
                return false;
            }

            type = field.Type;
        }

        return true;
    }

    private LookupOperation CreateLookupOperation(
        string schemaName,
        Lookup lookup,
        CompositeComplexType entityType,
        SelectionPlanNode parent,
        SelectionSetNode selectionSet)
    {
        var lookupFieldNode = new FieldNode(
            new NameNode(lookup.Name),
            null,
            [],
            [],
            selectionSet);

        var selectionNodes = new ISelectionNode[] { lookupFieldNode };

        var lookupFieldPlan = new FieldPlanNode(
            lookupFieldNode,
            new OutputFieldInfo(
                lookup.Name,
                entityType,
                ImmutableArray<string>.Empty.Add(schemaName)));

        var lookupOperation = new OperationPlanNode(
            schemaName,
            schema.QueryType,
            selectionNodes,
            parent);

        lookupOperation.AddSelection(lookupFieldPlan);

        return new LookupOperation(lookupOperation, lookupFieldPlan);
    }

    private static Dictionary<string, int> GetSchemasWeighted(
        CompositeComplexType operationType,
        SelectionSetNode selectionSet)
    {
        // this is to simplified ... we need to traverse instead
        var counts = new Dictionary<string, int>();
        var selectionBacklog = new Queue<ISelectionNode>(selectionSet.Selections);
        var visitedSelections = new HashSet<ISelectionNode>(SyntaxComparer.BySyntax);

        while (selectionBacklog.TryDequeue(out var selectionNode))
        {
            if (selectionNode is FieldNode fieldNode)
            {
                var field = operationType.Fields[fieldNode.Name.Value];

                foreach (var schemaName in field.Sources.Schemas)
                {
                    if (counts.TryGetValue(schemaName, out var count))
                    {
                        counts[schemaName] = count + 1;
                    }
                    else
                    {
                        counts[schemaName] = 1;
                    }
                }
            }
            else if (selectionNode is InlineFragmentNode inlineFragmentNode)
            {
                foreach (var selection in inlineFragmentNode.SelectionSet.Selections)
                {
                    if (visitedSelections.Add(selection))
                    {
                        selectionBacklog.Enqueue(selection);
                    }
                }
            }
        }

        return counts;
    }

    private static FieldPath CreateFieldPath(ImmutableStack<SelectionPlanNode> path)
    {
        var current = FieldPath.Root;

        foreach (var segment in path.Reverse())
        {
            if (segment is FieldPlanNode field)
            {
                current = current.Append(field.Field.Name);
            }
        }

        return current;
    }

    private static FieldNode CreateFieldNodeFromPath(FieldPath path)
    {
        var current = new FieldNode(path.Name);

        foreach (var segment in path.Skip(1))
        {
            current = new FieldNode(
                null,
                new NameNode(segment.Name),
                null,
                Array.Empty<DirectiveNode>(),
                Array.Empty<ArgumentNode>(),
                new SelectionSetNode([current]));
        }

        return current;
    }

    private void TryMakeOperationConditional(
        OperationPlanNode operation,
        IReadOnlyList<SelectionPlanNode> selections)
    {
        var firstSelection = selections.FirstOrDefault();
        if (firstSelection?.IsConditional != true)
        {
            return;
        }

        foreach (var selection in selections.Skip(1))
        {
            // if any root selection of an operation node has no condition
            // the operation will always be executed.
            if (!selection.IsConditional)
            {
                return;
            }

            if (!string.Equals(firstSelection.SkipVariable, selection.SkipVariable, StringComparison.Ordinal)
                || !string.Equals(firstSelection.IncludeVariable, selection.IncludeVariable, StringComparison.Ordinal))
            {
                return;
            }
        }

        operation.SkipVariable = firstSelection.SkipVariable;
        operation.IncludeVariable = firstSelection.IncludeVariable;

        var remove = new List<CompositeDirective>();

        foreach (var selection in selections)
        {
            selection.SkipVariable = null;
            selection.IncludeVariable = null;

            remove.AddRange(
                selection.Directives.Where(
                    t => t.Type == schema.SkipDirective || t.Type == schema.IncludeDirective));

            foreach (var directive in remove)
            {
                selection.RemoveDirective(directive);
            }
        }
    }

    private static bool IsSelectionAlwaysSkipped(ISelectionNode selectionNode)
    {
        foreach (var directive in selectionNode.Directives)
        {
            var isSkipDirective = directive.Name.Value == "skip";
            var isIncludedDirective = directive.Name.Value == "include";

            if (!isSkipDirective && !isIncludedDirective)
            {
                continue;
            }

            var ifArgument = directive.Arguments.FirstOrDefault(a => a.Name.Value == "if");

            if (ifArgument?.Value is not BooleanValueNode booleanValueNode)
            {
                continue;
            }

            if ((isSkipDirective && booleanValueNode.Value)
                || (isIncludedDirective && !booleanValueNode.Value))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasLookup(CompositeComplexType type)
        => type.IsEntity() || ReferenceEquals(type, schema.QueryType);

    private string GetNextRequirementName()
        => $"__fusion_requirement_{++_lastRequirementId}";

    private record struct LookupOperation(
        OperationPlanNode Operation,
        FieldPlanNode Field);

    private readonly record struct PlaningContext
    {
        public PlaningContext(
            OperationPlanNode operation)
            : this(operation, operation, ImmutableStack<SelectionPlanNode>.Empty)
        {
        }

        public PlaningContext(
            OperationPlanNode operation,
            SelectionPlanNode parent,
            ImmutableStack<SelectionPlanNode>? path = null)
        {
            Operation = operation;
            Parent = parent;
            Path = path ?? ImmutableStack<SelectionPlanNode>.Empty.Push(parent);
        }

        public OperationPlanNode Operation { get; init; }
        public SelectionPlanNode Parent { get; init; }
        public ImmutableStack<SelectionPlanNode> Path { get; init; }

        public void Deconstruct(
            out OperationPlanNode operation,
            out SelectionPlanNode parent,
            out ImmutableStack<SelectionPlanNode> path)
        {
            operation = Operation;
            parent = Parent;
            path = Path;
        }
    }
}
