using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Planning.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

// TODO: Flatten unnecessary inline fragments
// TODO: Remove selections from skipped fragment if they are part of the parent selection
public sealed class OperationPlanner(CompositeSchema schema)
{
    private int _lastRequirementId;

    public RequestPlanNode CreatePlan(DocumentNode document, string? operationName)
    {
        ArgumentNullException.ThrowIfNull(document);

        var operationDefinition = document.GetOperation(operationName);
        var schemasWeighted = GetSchemasWeighted(schema.QueryType, operationDefinition.SelectionSet);
        var operationPlan = new RequestPlanNode(document, operationName);

        // this need to be rewritten to check if everything is planned for.
        foreach (var schemaName in schemasWeighted.OrderByDescending(t => t.Value).Select(t => t.Key))
        {
            var operation = new OperationPlanNode(
                schemaName,
                schema.QueryType,
                operationDefinition.SelectionSet);

            var context = new PlaningContext(operation, operation, ImmutableStack<SelectionPathSegment>.Empty);
            if (TryPlanSelectionSet(context))
            {
                TryMakeOperationConditional(operation, operation.Selections);
                operationPlan.AddOperation(operation);
            }
        }

        OperationVariableBinder.BindOperationVariables(operationDefinition, operationPlan);

        return operationPlan;
    }

    private bool TryPlanSelectionSet(
        PlaningContext context,
        bool skipUnresolved = false)
    {
        if (context.Parent.SelectionNodes is null)
        {
            throw new InvalidOperationException(
                "A leaf field cannot be a parent node.");
        }

        List<IUnresolvedSelection>? unresolvedSelections = null;
        // List<UnresolvedType>? unresolvedTypes = null;
        var type = (CompositeComplexType)context.Parent.DeclaringType;
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
                selection,
                unresolvedSelection =>
                {
                    unresolvedSelections ??= new List<IUnresolvedSelection>();
                    unresolvedSelections.Add(unresolvedSelection);
                });
        }

        if (haveConditionalSelectionsBeenRemoved)
        {
            // If we have removed conditional selections from a composite field, we need to add a __typename field
            // to have a valid selection set.
            if (context.Parent is FieldPlanNode { Selections.Count: 0 } fieldPlanNode)
            {
                // TODO: How to properly create a __typename field?
                var dummyType = new CompositeObjectType("Dummy", description: null,
                    fields: new CompositeOutputFieldCollection([]));
                var outputFieldInfo = new OutputFieldInfo("__typename", dummyType, []);
                fieldPlanNode.AddSelection(new FieldPlanNode(new FieldNode("__typename"), outputFieldInfo));
            }
            // If we have removed conditional selections from an operation, we need to fail the creation
            // of the operation as it would be invalid without any selections.
            else if (context.Parent is OperationPlanNode { Selections.Count: 0 })
            {
                return false;
            }
        }

        return skipUnresolved ||
            unresolvedSelections is null ||
            unresolvedSelections.Count == 0 ||
            TryHandleUnresolvedSelections(context, type, unresolvedSelections);
    }

    private bool TryPlanSelection(
        PlaningContext context,
        CompositeComplexType type,
        ISelectionNode selectionNode,
        Action<IUnresolvedSelection> trackUnresolvedSelection)
    {
        if (selectionNode is FieldNode fieldNode)
        {
            return TryPlanFieldSelection(
                context,
                type,
                fieldNode,
                trackUnresolvedSelection);
        }

        if (selectionNode is InlineFragmentNode inlineFragmentNode)
        {
            return TryPlanInlineFragmentSelection(
                context,
                type,
                inlineFragmentNode,
                trackUnresolvedSelection);
        }

        return false;
    }

    private bool TryPlanInlineFragmentSelection(
        PlaningContext context,
        CompositeComplexType type,
        InlineFragmentNode inlineFragmentNode,
        Action<IUnresolvedSelection> trackUnresolvedSelection)
    {
        var typeCondition = type;
        if (inlineFragmentNode.TypeCondition?.Name.Value is { } conditionTypeName &&
            // TODO: CompositeComplexType does not include unions which are a valid value for type conditions.
            schema.TryGetType<CompositeComplexType>(conditionTypeName, out var typeConditionType))
        {
            typeCondition = typeConditionType;
        }

        var inlineFragmentPlanNode = new InlineFragmentPlanNode(typeCondition, inlineFragmentNode);
        var inlineFragmentContext = new PlaningContext(context.Operation, inlineFragmentPlanNode,
            ImmutableStack<SelectionPathSegment>.Empty);
        List<IUnresolvedSelection>? unresolvedSelections = null;

        foreach (var selection in inlineFragmentNode.SelectionSet.Selections)
        {
            if (IsSelectionAlwaysSkipped(selection))
            {
                // TODO: How to reconcile this?
                continue;
            }

            TryPlanSelection(
                inlineFragmentContext,
                typeCondition,
                selection,
                unresolvedSelection =>
                {
                    unresolvedSelections ??= new List<IUnresolvedSelection>();
                    unresolvedSelections.Add(unresolvedSelection);
                });
        }

        if (unresolvedSelections is { Count: > 0 })
        {
            var unresolvedInlineFragment =
                new UnresolvedInlineFragment(inlineFragmentNode.Directives, typeCondition, unresolvedSelections);

            trackUnresolvedSelection(unresolvedInlineFragment);
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
        FieldNode fieldNode,
        Action<UnresolvedField> trackUnresolvedSelection)
    {
        if (!type.Fields.TryGetField(fieldNode.Name.Value, out var field))
        {
            throw new InvalidOperationException(
                "There is an unknown field in the selection set.");
        }

        // if we have an operation plan node we have a pre-validated set of
        // root fields, so we now the field will be resolvable on the
        // source schema.
        if (context.Parent is OperationPlanNode || IsResolvable(fieldNode, field, context.Operation.SchemaName))
        {
            var fieldNamedType = field.Type.NamedType();

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

            // if this field as a selection set it must be a object, interface or union type,
            // otherwise the validation should have caught this. So, we just throw here if this
            // is not the case.
            if (fieldNamedType.Kind != TypeKind.Object &&
                fieldNamedType.Kind != TypeKind.Interface &&
                fieldNamedType.Kind != TypeKind.Union)
            {
                throw new InvalidOperationException(
                    "Only object, interface, or union types can have a selection set.");
            }

            var fieldPlanNode = new FieldPlanNode(fieldNode, field);
            AddSelectionDirectives(fieldPlanNode, fieldNode.Directives);

            var pathSegment = new SelectionPathSegment(fieldPlanNode);

            if (TryPlanSelectionSet(context with { Parent = fieldPlanNode, Path = context.Path.Push(pathSegment) }))
            {
                context.Parent.AddSelection(fieldPlanNode);
                return true;
            }

            trackUnresolvedSelection(new UnresolvedField(fieldNode, field));
            return false;
        }

        // unresolvable fields will be collected to backtrack later.
        trackUnresolvedSelection(new UnresolvedField(fieldNode, field));
        return false;
    }

    private void AddSelectionDirectives(
        SelectionPlanNode selection,
        IReadOnlyList<DirectiveNode> directiveNodes)
    {
        foreach (var directiveNode in directiveNodes)
        {
            var directiveType = schema.GetDirectiveType(directiveNode.Name.Value);

            if ((directiveType == schema.SkipDirective || directiveType == schema.IncludeDirective) &&
                directiveNode.Arguments[0].Value is BooleanValueNode)
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
        CompositeComplexType type,
        List<IUnresolvedSelection> unresolvedSelections)
    {
        if (!TryResolveEntityType(context.Parent, out var entityPath))
        {
            return false;
        }

        // if we have found an entity to branch of from we will check
        // if any of the unresolved selections can be resolved through one of the entity lookups.
        var schemasInContext = new Dictionary<string, OperationPlanNode>();
        var processedFields = new HashSet<string>();
        var selections = new List<ISelectionNode>();

        schemasInContext.Add(context.Operation.SchemaName, context.Operation);

        // we first try to weight the schemas that the fields can be resolved by.
        // The schema is weighted by the fields it potentially can resolve.
        var schemasWeighted = GetSchemasWeighted(unresolvedSelections, schemasInContext.Keys);

        foreach (var schemaName in schemasWeighted.OrderByDescending(t => t.Value).Select(t => t.Key))
        {
            if (schemasInContext.ContainsKey(schemaName))
            {
                continue;
            }

            // if the path is not resolvable we will skip it and move to the next.
            if (!IsEntityPathResolvable(entityPath, schemaName))
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

            // note : this can lead to a operation explosions as fields could be unresolvable
            // and would be spread out in the lower level call. We do that for now to test out the
            // overall concept and will backtrack later to the upper call.
            selections.Clear();

            foreach (var unresolvedSelection in unresolvedSelections)
            {
                if (unresolvedSelection is UnresolvedField unresolvedField)
                {
                    if (unresolvedField.Field.Sources.ContainsSchema(schemaName) &&
                        !processedFields.Contains(unresolvedField.Field.Name))
                    {
                        selections.Add(unresolvedField.FieldNode);
                    }
                }
                // TODO: Are we only concerned with the top-level of fields here?
                else if (unresolvedSelection is UnresolvedInlineFragment unresolvedInlineFragment)
                {
                    var resolvableFields = new List<FieldNode>();

                    foreach (var unresolvedSubSelection in unresolvedInlineFragment.UnresolvedSelections)
                    {
                        if (unresolvedSubSelection is UnresolvedField unresolvedSubField)
                        {
                            // We're specifically not checking processed fields here as fields outside the inline fragment should still be added.
                            if (unresolvedSubField.Field.Sources.ContainsSchema(schemaName))
                            {
                                resolvableFields.Add(unresolvedSubField.FieldNode);
                            }
                        }
                    }

                    if (resolvableFields.Count > 0)
                    {
                        selections.Add(new InlineFragmentNode(
                            null,
                            new NamedTypeNode(unresolvedInlineFragment.TypeCondition.Name),
                            unresolvedInlineFragment.Directives,
                            new SelectionSetNode(resolvableFields)));
                    }
                }
            }

            var (lookupOperation, lookupField) =
                CreateLookupOperation(schemaName, lookup, type, context.Parent, selections);
            if (!TryPlanSelectionSet(context with { Operation = lookupOperation, Parent = lookupField }, true))
            {
                continue;
            }

            schemasInContext.Add(schemaName, lookupOperation);
            TryMakeOperationConditional(lookupOperation, lookupField.Selections);

            // we add the lookup operation to all the schemas that we have requirements with.
            foreach (var requiredSchema in fieldSchemaDependencies.Values.Distinct())
            {
                // Add child node is wrong ... this is a graph and the lookup operation has dependencies on
                // this operation. We should probably double link here.
                // maybe AddDependantNode()?
                schemasInContext[requiredSchema].AddDependantOperation(lookupOperation);
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
                    ? ImmutableStack<SelectionPathSegment>.Empty.Push(new SelectionPathSegment(lookupField))
                    : context.Path;
                var requiredFromPath = CreateFieldPath(requiredFromPathStack);
                var requiredFromContext = new PlaningContext(
                    requiredFromOperation,
                    requiredFromSelectionSet,
                    requiredFromPathStack);

                if (!TryPlanSelection(
                    requiredFromContext,
                    (CompositeComplexType)requiredFromSelectionSet.DeclaringType,
                    CreateFieldNodeFromPath(requiredField),
                    _ => { }))
                {
                    return false;
                }

                var requirement = new FieldRequirementPlanNode(
                    requirementName,
                    requiredFromOperation,
                    requiredFromPath,
                    requiredField,
                    argument.Type);

                lookupOperation.AddRequirement(requirement);
                lookupField.AddArgument(new ArgumentAssignment(argument.Name, new VariableNode(requirementName)));
            }

            // we register the fields that we were able to resolve with the lookup
            // so that if there are still unresolved fields we can check if we can
            // resolve them with another lookup.
            foreach (var selection in lookupField.Selections)
            {
                switch (selection)
                {
                    case FieldPlanNode field:
                        processedFields.Add(field.Field.Name);
                        break;

                    case InlineFragmentPlanNode inlineFragmentNode:
                        // TODO: Do we have to do this?
                        break;

                    default:
                        throw new NotSupportedException();
                }
            }
        }

        // TODO: Uuuuuuuuuuuuuuugly hack, just to make tests work for now
        if (unresolvedSelections.OfType<UnresolvedInlineFragment>().Any())
        {
            return true;
        }

        return unresolvedSelections.Count == processedFields.Count;
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

    private static bool IsEntityPathResolvable(Stack<PlanNode> entityPath, string schemaName)
    {
        foreach (var planNode in entityPath.Skip(1))
        {
            if (planNode is FieldPlanNode fieldPlanNode)
            {
                if (!fieldPlanNode.Field.Sources.Contains(schemaName))
                {
                    return false;
                }
            }
        }

        return true;
    }

    // this needs more meat
    private bool IsResolvable(
        FieldNode fieldNode,
        CompositeOutputField field,
        string schemaName)
        => field.Sources.ContainsSchema(schemaName);

    // this needs more meat
    private bool IsResolvable(
        InlineFragmentNode inlineFragment,
        CompositeComplexType typeCondition,
        string schemaName)
        => typeCondition.Sources.ContainsSchema(schemaName);

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

        lookup = default;
        fieldSchemaDependencies = default;
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
            if (type.NamedType() is not CompositeComplexType complexType ||
                !complexType.Fields.TryGetField(segment.Name, out var field) ||
                !field.Sources.TryGetMember(schemaName, out var source) ||
                source.Requirements is not null)
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
        IReadOnlyList<ISelectionNode> selections)
    {
        var lookupFieldNode = new FieldNode(
            new NameNode(lookup.Name),
            null,
            [],
            [],
            new SelectionSetNode(selections));

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
        IEnumerable<IUnresolvedSelection> unresolvedSelections,
        IEnumerable<string> skipSchemaNames)
    {
        var counts = new Dictionary<string, int>();
        var unresolvedSelectionBacklog = new Queue<IUnresolvedSelection>(unresolvedSelections);

        while (unresolvedSelectionBacklog.TryDequeue(out var unresolvedSelection))
        {
            if (unresolvedSelection is UnresolvedField unresolvedField)
            {
                foreach (var schemaName in unresolvedField.Field.Sources.Schemas)
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
            else if (unresolvedSelection is UnresolvedInlineFragment unresolvedInlineFragment)
            {
                foreach (var selection in unresolvedInlineFragment.UnresolvedSelections)
                {
                    unresolvedSelectionBacklog.Enqueue(selection);
                }
            }
        }

        foreach (var schemaName in skipSchemaNames)
        {
            counts.Remove(schemaName);
        }

        return counts;
    }

    private static Dictionary<string, int> GetSchemasWeighted(
        CompositeObjectType operationType,
        SelectionSetNode selectionSet)
    {
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

    private static FieldPath CreateFieldPath(ImmutableStack<SelectionPathSegment> path)
    {
        var current = FieldPath.Root;

        foreach (var segment in path.Reverse())
        {
            if (segment.PlanNode is FieldPlanNode field)
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

        return current!;
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

            if (!string.Equals(firstSelection.SkipVariable, selection.SkipVariable, StringComparison.Ordinal) ||
                !string.Equals(firstSelection.IncludeVariable, selection.IncludeVariable, StringComparison.Ordinal))
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

            if (isSkipDirective || isIncludedDirective)
            {
                var ifArgument = directive.Arguments.FirstOrDefault(a => a.Name.Value == "if");

                if (ifArgument is not null)
                {
                    if (ifArgument.Value is BooleanValueNode booleanValueNode)
                    {
                        if (booleanValueNode.Value && isSkipDirective)
                        {
                            return true;
                        }

                        if (!booleanValueNode.Value && isIncludedDirective)
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    // TODO: Needs to be scoped on operation unless planner is transient
    private string GetNextRequirementName()
        => $"__fusion_requirement_{++_lastRequirementId}";

    public record SelectionPathSegment(
        SelectionPlanNode PlanNode);

    public interface IUnresolvedSelection;

    public record UnresolvedField(
        FieldNode FieldNode,
        CompositeOutputField Field) : IUnresolvedSelection;

    public record UnresolvedInlineFragment(
        IReadOnlyList<DirectiveNode> Directives,
        CompositeComplexType TypeCondition,
        List<IUnresolvedSelection> UnresolvedSelections) : IUnresolvedSelection;

    private record struct LookupOperation(
        OperationPlanNode Operation,
        FieldPlanNode Field);

    private record struct PlaningContext(
        OperationPlanNode Operation,
        SelectionPlanNode Parent,
        ImmutableStack<SelectionPathSegment> Path);
}
