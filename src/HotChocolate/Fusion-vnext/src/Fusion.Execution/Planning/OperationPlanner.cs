using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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

        var operationDefinition = document.GetOperation(operationName);
        var schemasWeighted = GetSchemasWeighted(schema.QueryType, operationDefinition.SelectionSet);
        var operationPlan = new RootPlanNode();

        // this need to be rewritten to check if everything is planned for.
        foreach (var schemaName in schemasWeighted.OrderByDescending(t => t.Value).Select(t => t.Key))
        {
            var operation = new OperationPlanNode(
                schemaName,
                schema.QueryType,
                operationDefinition.SelectionSet);

            if (TryPlanSelectionSet(operation, operation, new Stack<SelectionPathSegment>()))
            {
                operationPlan.AddChildNode(operation);
            }
        }

        OperationVariableBinder.BindOperationVariables(operationDefinition, operationPlan);

        return operationPlan;
    }

    private bool TryPlanSelectionSet(
        OperationPlanNode operation,
        SelectionPlanNode parent,
        Stack<SelectionPathSegment> path,
        bool skipUnresolved = false)
    {
        if (parent.SelectionNodes is null)
        {
            throw new InvalidOperationException(
                "A leaf field cannot be a parent node.");
        }

        List<UnresolvedField>? unresolved = null;
        var type = (CompositeComplexType)parent.DeclaringType;

        foreach (var selection in parent.SelectionNodes)
        {
            if (selection is FieldNode fieldNode)
            {
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

                    if (TryPlanSelectionSet(operation, fieldPlanNode, path))
                    {
                        parent.AddSelection(fieldPlanNode);
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

        return skipUnresolved
            || unresolved is null
            || unresolved.Count == 0
            || TryHandleUnresolvedSelections(operation, parent, type, unresolved, path);
    }

    private bool TryHandleUnresolvedSelections(
        OperationPlanNode operation,
        SelectionPlanNode parent,
        CompositeComplexType type,
        List<UnresolvedField> unresolved,
        Stack<SelectionPathSegment> path)
    {
        if (!TryResolveEntityType(parent, out var entityPath))
        {
            return false;
        }

        // if we have found an entity to branch of from we will check
        // if any of the unresolved selections can be resolved through one of the entity lookups.
        var processedSchemas = new HashSet<string>();
        var processedFields = new HashSet<string>();
        var fields = new List<ISelectionNode>();

        // we first try to weight the schemas that the fields can be resolved by.
        // The schema is weighted by the fields it potentially can resolve.
        var schemasWeighted = GetSchemasWeighted(unresolved, processedSchemas);

        foreach (var schemaName in schemasWeighted.OrderByDescending(t => t.Value).Select(t => t.Key))
        {
            if (!processedSchemas.Add(schemaName))
            {
                continue;
            }

            // if the path is not resolvable we will skip it and move to the next.
            if (!IsEntityPathResolvable(entityPath, schemaName))
            {
                continue;
            }

            // next we try to find a lookup
            if (!TryGetLookup((SelectionPlanNode)entityPath.Peek(), processedSchemas, out var lookup))
            {
                continue;
            }

            // note : this can lead to a operation explosions as fields could be unresolvable
            // and would be spread out in the lower level call. We do that for now to test out the
            // overall concept and will backtrack later to the upper call.
            fields.Clear();

            foreach (var unresolvedField in unresolved)
            {
                if (unresolvedField.Field.Sources.ContainsSchema(schemaName)
                    && !processedFields.Contains(unresolvedField.Field.Name))
                {
                    fields.Add(unresolvedField.FieldNode);
                }
            }

            var lookupOperation = CreateLookupOperation(schemaName, lookup, type, parent, fields);
            var lookupField = lookupOperation.Selections[0];

            // what do we do of its not successful
            if (!TryPlanSelectionSet(lookupOperation, lookupField, path))
            {
                continue;
            }

            operation.AddChildNode(lookupOperation);

            foreach (var selection in lookupField.Selections)
            {
                switch (selection)
                {
                    case FieldPlanNode field:
                        processedFields.Add(field.Field.Name);
                        break;

                    default:
                        throw new NotSupportedException();
                }
            }
        }

        return unresolved.Count == processedFields.Count;
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

    private bool TryGetLookup(SelectionPlanNode selection, HashSet<string> schemas, out Lookup lookup)
    {
        // we need a helper here that can take lookups from interfaces
        // also this is a simplified selection of a lookup ... we have to take into account what data
        // is available for free.
        foreach (var schemaName in schemas)
        {
            if (((CompositeComplexType)selection.DeclaringType).Sources.TryGetType(schemaName, out var source)
                && source.Lookups.Length > 0)
            {
                lookup = source.Lookups[0];
                return true;
            }
        }

        throw new NotImplementedException();
    }

    private OperationPlanNode CreateLookupOperation(
        string schemaName,
        Lookup lookup,
        CompositeComplexType entityType,
        SelectionPlanNode parent,
        IReadOnlyList<ISelectionNode> selections)
    {
        var lookupFieldNode = new FieldNode(
            new NameNode(lookup.Name),
            null,
            Array.Empty<DirectiveNode>(),
            Array.Empty<ArgumentNode>(),
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

        return lookupOperation;
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
