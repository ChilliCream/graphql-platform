using HotChocolate.Fusion.Planning.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

public sealed class OperationPlanner(CompositeSchema schema)
{
    public RootPlanNode CreatePlan(DocumentNode document, string? operationName)
    {
        ArgumentNullException.ThrowIfNull(document);

        var operationDefinition =  document.GetOperation(operationName);
        var schemasWeighted = GetSchemasWeighted(schema.QueryType, operationDefinition.SelectionSet);
        var rootPlanNode = new RootPlanNode();

        // this need to be rewritten to check if everything is planned for.
        foreach (var schemaName in schemasWeighted.OrderByDescending(t => t.Value).Select(t => t.Key))
        {
            var operation = new OperationPlanNode(
                schemaName,
                schema.QueryType,
                operationDefinition.SelectionSet);

            if (TryResolveSelectionSet(operation, operation, new Stack<SelectionPathSegment>()))
            {
                rootPlanNode.AddOperation(operation);
            }
        }

        return rootPlanNode;
    }

    private bool TryResolveSelectionSet(
        OperationPlanNode operation,
        SelectionPlanNode parent,
        Stack<SelectionPathSegment> path)
    {
        if (parent.SelectionNodes is null)
        {
            throw new InvalidOperationException(
                "A leaf field cannot be a parent node.");
        }

        List<UnresolvedField>? unresolved = null;
        CompositeComplexType? type = null;
        var areAnySelectionsResolvable = false;

        foreach (var selection in parent.SelectionNodes)
        {
            if (selection is FieldNode fieldNode)
            {
                type ??= (CompositeComplexType)parent.DeclaringType;

                if (!type.Fields.TryGetField(fieldNode.Name.Value, out var field))
                {
                    throw new InvalidOperationException(
                        "There is an unknown field in the selection set.");
                }

                // if we have an operation plan node we have a pre-validated set of
                // root fields, so we now the field will be resolvable on the
                // source schema.
                if (parent is OperationPlanNode
                    || IsResolvable(fieldNode, field, operation.SchemaName))
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

                        parent.AddSelection(new FieldPlanNode(fieldNode, field));
                        areAnySelectionsResolvable = true;
                        continue;
                    }

                    // if this field as a selection set it must be a object, interface or union type,
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
                    var pathSegment = new SelectionPathSegment(fieldPlanNode);

                    path.Push(pathSegment);

                    if (TryResolveSelectionSet(operation, fieldPlanNode, path))
                    {
                        parent.AddSelection(fieldPlanNode);
                        areAnySelectionsResolvable = true;
                    }
                    else
                    {
                        unresolved ??= [];
                        unresolved.Add(new UnresolvedField(fieldNode, field, parent));
                    }

                    path.Pop();
                }
                else
                {
                    // unresolvable fields will be collected to backtrack later.
                    unresolved ??= [];
                    unresolved.Add(new UnresolvedField(fieldNode, field, parent));
                }
            }
        }

        if (unresolved?.Count > 0)
        {
            var current = parent;
            var unresolvedPath = new Stack<PlanNode>();
            unresolvedPath.Push(parent);

            // first we try to find an entity from which we can branch.
            // We go up until we find the first entity.
            while (!current.IsEntity
                && current.Parent is SelectionPlanNode parentSelection)
            {
                current = parentSelection;
                unresolvedPath.Push(current);
            }

            // If we could not find an entity we cannot resolve the unresolved selections.
            if (!current.IsEntity)
            {
                // TODO: there is a case where we do root selections on data, we will ignore it for now.
                return false;
            }

            // if we have found an entity to branch of from we will check
            // if any of the unresolved selections can be resolved through one of the entity lookups.
            var processed = new HashSet<string>();

            // we first try to weight the schemas that the fields can be resolved by.
            // The schema is weighted by the fields it potentially can resolve.
            var schemasWeighted = GetSchemasWeighted(unresolved, processed);

            foreach (var schemaName in schemasWeighted.OrderByDescending(t => t.Value).Select(t => t.Key))
            {
                if (processed.Add(schemaName))
                {
                    var isPathResolvable = true;

                    // a possible schema must be able to resolve the path to the lookup.
                    foreach (var pathSegment in unresolvedPath.Skip(1))
                    {
                        if (pathSegment is FieldPlanNode selection
                            && selection.Field is not null
                            && selection.Field.Sources.ContainsSchema(schemaName))
                        {
                            continue;
                        }

                        isPathResolvable = true;
                        break;
                    }

                    // if the path is not resolvable we will skip it and move to the next.
                    if (!isPathResolvable)
                    {
                        continue;
                    }

                    // next we try to find a lookup
                    if (TryGetLookup(current, processed, out var lookup))
                    {
                        // note : this can lead to a operation explosions as fields could be unresolvable
                        // and would be spread out in the lower level call. We do that for now to test out the
                        // overall concept and will backtrack later to the upper call.
                        var fields = new List<ISelectionNode>();

                        foreach (var unresolvedField in unresolved)
                        {
                            if (unresolvedField.Field.Sources.ContainsSchema(schemaName))
                            {
                                fields.Add(unresolvedField.FieldNode);
                            }
                        }

                        var newOperation = new OperationPlanNode(
                            schemaName,
                            schema.QueryType,
                            CreateLookupSelections(lookup, parent, fields),
                            parent);

                        // what do we do of its not successful
                        if (TryResolveSelectionSet(newOperation, newOperation, path))
                        {
                            operation.AddOperation(newOperation);
                        }
                    }
                }
            }
        }

        return areAnySelectionsResolvable;
    }

    // this needs more meat
    private bool IsResolvable(
        FieldNode fieldNode,
        CompositeOutputField field,
        string schemaName)
        => field.Sources.ContainsSchema(schemaName);

    private bool TryGetLookup(SelectionPlanNode selection, HashSet<string> schemas, out Lookup lookup)
    {
        // we need a helper here that can take lookups from interfaces
        // also this is a simplified selection of a lookup ... we have to take into account what data
        // is available for free.
        foreach (var schemaName in schemas)
        {
            if (((CompositeObjectType)selection.DeclaringType).Sources.TryGetMember(schemaName, out var source)
                && source.Lookups.Length > 0)
            {
                lookup = source.Lookups[0];
                return true;
            }
        }

        throw new NotImplementedException();
    }

    private IReadOnlyList<ISelectionNode> CreateLookupSelections(
        Lookup lookup,
        SelectionPlanNode parent,
        IReadOnlyList<ISelectionNode> selections)
    {
        return
        [
            new FieldNode(
                new NameNode(lookup.Name),
                null,
                Array.Empty<DirectiveNode>(),
                Array.Empty<ArgumentNode>(),
                new SelectionSetNode(selections))
        ];
    }


    private static Dictionary<string, int> GetSchemasWeighted(
        IEnumerable<UnresolvedField> unresolvedFields,
        IEnumerable<string> skipSchemaNames)
    {
        var counts = new Dictionary<string, int>();

        foreach (var unresolvedField in unresolvedFields)
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

        foreach (var selectionNode in selectionSet.Selections)
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
        }

        return counts;
    }

    public record SelectionPathSegment(
        SelectionPlanNode PlanNode);

    public record UnresolvedField(
        FieldNode FieldNode,
        CompositeOutputField Field,
        SelectionPlanNode Parent);

    public class RequestPlanNode
    {
        public ICollection<OperationPlanNode> Operations { get; } = new List<OperationPlanNode>();
    }
}
