using System.Collections.Immutable;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

public sealed class OperationPlanner2(CompositeSchema schema)
{
    public void CreatePlan(DocumentNode document, string? operationName)
    {
        throw new NotImplementedException();
    }

    public bool TryResolveSelectionSet(
        OperationPlanNode operation,
        ISelectionPlanNode parent,
        Stack<SelectionSetContext> path)
    {
        List<UnresolvedField> unresolved = default!;
        CompositeComplexType? type = null;
        var areAnySelectionsResolvable = false;

        foreach (var selection in parent.SelectionNodes)
        {
            if (selection is FieldNode fieldNode)
            {
                type ??= (CompositeComplexType)parent.Type;

                if (!type.Fields.TryGetField(fieldNode.Name.Value, out var field))
                {
                    throw new InvalidOperationException(
                        "There is an unknown field in the selection set.");
                }

                if (IsResolvable(fieldNode, field, operation.SchemaName))
                {
                    var fieldNamedType = field.Type.NamedType();

                    // if the field has no selection set it must be a leaf type.
                    // This also means that if this field is resolvable that we can
                    // just include it and no further processing is required.
                    if (fieldNode.SelectionSet is null)
                    {
                        if (fieldNamedType.Kind is TypeKind.Enum or TypeKind.Scalar)
                        {
                            throw new InvalidOperationException(
                                "Only complex types can have a selection set.");
                        }

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

                    var selectionSetContext = new SelectionSetContext
                    {
                        SyntaxNode = fieldNode.SelectionSet,
                        PlanNode = new SelectionPlanNode(
                            fieldNode.Alias?.Value ?? fieldNode.Name.Value,
                            fieldNode.SelectionSet,
                            fieldNamedType,
                            isEntity: false)
                    };

                    path.Push(selectionSetContext);

                    if (TryResolveSelectionSet(operation, selectionSetContext.PlanNode, path))
                    {
                        parent.AddSelection(selectionSetContext.PlanNode);
                        areAnySelectionsResolvable = true;
                    }
                    else
                    {
                        unresolved.Add(new UnresolvedField(fieldNode, field, parent));
                    }

                    path.Pop();
                }
                else
                {
                    // unresolvable fields will be collected to backtrack later.
                    unresolved.Add(new UnresolvedField(fieldNode, field, parent));
                }
            }
        }

        if (unresolved.Count > 0)
        {
            var current = parent;
            var unresolvedPath = new Stack<IQueryPlanNode>();
            unresolvedPath.Push(parent);

            // first we try to find an entity from which we can branch.
            // We go up until we find the first entity.
            while (!current.IsEntity
                && current.Parent is ISelectionPlanNode parentSelection)
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
                        if (pathSegment is SelectionPlanNode selection
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
                            fields,
                            parent.Type,
                            parent);

                        TryResolveSelectionSet(newOperation, newOperation, path);
                    }
                }
            }
        }

        return areAnySelectionsResolvable;
    }

    private bool IsResolvable(
        FieldNode fieldNode,
        CompositeOutputField field,
        string schemaName)
    {
        return false;
    }

    private bool TryGetLookup(ISelectionPlanNode selection, HashSet<string> schemas, out Lookup lookup)
    {
        throw new NotImplementedException();
    }

    private FieldNode


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

    public class SelectionSetContext
    {
        public SelectionSetNode SyntaxNode { get; set; } = default!;

        public SelectionPlanNode PlanNode { get; set; } = default!;
    }

    public record UnresolvedField(FieldNode FieldNode, CompositeOutputField Field, ISelectionPlanNode Parent);

    public class RequestPlanNode
    {
        public ICollection<OperationPlanNode> Operations { get; } = new List<OperationPlanNode>();
    }

    public sealed class OperationPlanNode : ISelectionPlanNode
    {
        private List<ISelectionPlanNode>? _selections;
        private List<CompositeDirective>? _directives;

        public OperationPlanNode(
            string schemaName,
            SelectionSetNode selectionSet,
            ICompositeNamedType type,
            IQueryPlanNode? parent = null)
        {
            Parent = parent;
            SchemaName = schemaName;
            SelectionNodes = selectionSet.Selections;
            Type = type;
        }

        public OperationPlanNode(
            string schemaName,
            IReadOnlyList<ISelectionNode> selections,
            ICompositeNamedType type,
            IQueryPlanNode? parent = null)
        {
            Parent = parent;
            SchemaName = schemaName;
            SelectionNodes = selections;
            Type = type;
        }

        public IQueryPlanNode? Parent { get; }

        public string SchemaName { get; }

        public ICompositeNamedType Type { get; }

        public bool IsEntity => false;

        public IReadOnlyList<ISelectionPlanNode> Selections
            => _selections ??= [];

        public IReadOnlyList<ISelectionNode> SelectionNodes { get; }

        public void AddSelection(ISelectionPlanNode selection)
        {
            ArgumentNullException.ThrowIfNull(selection);
            (_selections ??= []).Add(selection);
        }

        public IReadOnlyList<CompositeDirective> Directives
            => _directives ??= [];

        public void AddDirective(CompositeDirective selection)
        {
            ArgumentNullException.ThrowIfNull(selection);
            (_directives ??= []).Add(selection);
        }
    }

    public class SelectionPlanNode : ISelectionPlanNode
    {
        private List<CompositeDirective>? _directives;
        private List<ArgumentAssignment>? _arguments;

        public SelectionPlanNode(
            string responseName,
            SelectionSetNode syntaxNode,
            ICompositeNamedType type,
            bool isEntity)
        {
            ResponseName = responseName;
            SyntaxNode = syntaxNode;
            Type = type;
            IsEntity = isEntity;
        }

        public string ResponseName { get; }

        public CompositeOutputField? Field { get; set; }

        public SelectionSetNode SyntaxNode { get; }

        public ICompositeNamedType Type { get; }

        public bool IsEntity { get; }

        public IReadOnlyList<ArgumentAssignment> Arguments
        {
            get => _arguments ??= [];
        }

        public IReadOnlyList<CompositeDirective> Directives { get; }

        public IReadOnlyList<ISelectionPlanNode>? Selections { get; }

        public void AddSelection(ISelectionPlanNode selection)
        {
            throw new NotImplementedException();
        }

        public void AddDirective(CompositeDirective selection)
        {
            throw new NotImplementedException();
        }
    }

    public class ScopePlanNode : ISelectionPlanNode
    {
        private List<CompositeDirective>? _directives;

        public ScopePlanNode(
            SelectionSetNode syntaxNode,
            ICompositeNamedType type,
            bool isEntity)
        {
            SyntaxNode = syntaxNode;
            Type = type;
            IsEntity = isEntity;
        }

        public SelectionSetNode SyntaxNode { get; }

        public ICompositeNamedType Type { get; }

        public bool IsEntity { get; }

        public IReadOnlyList<ISelectionPlanNode>? Selections { get; }

        public IReadOnlyList<CompositeDirective> Directives { get; }

        public void AddSelection(ISelectionPlanNode selection)
        {
            throw new NotImplementedException();
        }

        public void AddDirective(CompositeDirective selection)
        {
            throw new NotImplementedException();
        }
    }

    public interface ISelectionProvider : IQueryPlanNode
    {
        ICompositeNamedType Type { get; }

        bool IsEntity { get; }

        IReadOnlyList<ISelectionPlanNode>? Selections { get; }

        IReadOnlyList<ISelectionNode> SelectionNodes  { get; }

        void AddSelection(ISelectionPlanNode selection);
    }

    public interface ISelectionPlanNode : ISelectionProvider
    {
        IReadOnlyList<CompositeDirective> Directives { get; }

        void AddDirective(CompositeDirective selection);
    }

    public interface IQueryPlanNode
    {
        IQueryPlanNode? Parent { get; }
    }
}

public sealed class OperationPlanner(CompositeSchema schema)
{
    public DocumentNode RewriteDocument(DocumentNode document, string? operationName)
    {
        var backlog = new Queue<SelectionSet>();
        var operation = document.GetOperation(operationName);
        var operationType = schema.GetOperationType(operation.Operation);
        var context = new Context("a", operationType, backlog, isRoot: true);

        // RewriteFields(operation.SelectionSet, context);

        var newSelectionSet = new SelectionSetNode(
            null,
            context.Selections.ToImmutable());

        var newOperation = new OperationDefinitionNode(
            null,
            operation.Name,
            operation.Operation,
            operation.VariableDefinitions,
            operation.Directives,
            newSelectionSet);

        return new DocumentNode(ImmutableArray<IDefinitionNode>.Empty.Add(newOperation));
    }

    private void CollectRootFields(
        SelectionSetNode selectionSet,
        CompositeObjectType operationType,
        Queue<SelectionSet> backlog)
    {
        foreach (var selection in selectionSet.Selections.OfType<FieldNode>())
        {
            var field = operationType.Fields[selection.Name.Value];

            foreach (var source in field.Sources.OrderByDescending(
                s => GetFieldCount(s.SchemaName, selection.SelectionSet!, field.Type.NamedType())))
            {
                var context = new Context(source.SchemaName, field.Type.NamedType(), backlog);
                CollectFields(selection.SelectionSet!, context);
            }
        }
    }

    private void CollectFields(SelectionSetNode selectionSet, Context context)
    {
        if (context.Type is CompositeComplexType complexType)
        {
            foreach (var selection in selectionSet.Selections)
            {
                switch (selection)
                {
                    case FieldNode field:
                        CollectField(field, complexType, context);
                        break;
                }
            }
        }
    }

    private void CollectField(FieldNode selection, CompositeComplexType complexType, Context context)
    {
        if (!complexType.Fields.TryGetField(selection.Name.Value, out var field)
            || !field.Sources.TryGetMember(context.SchemaName, out var sourceField))
        {
            context.EnqueueToBacklog(selection);
            return;
        }

        if (selection.SelectionSet is null)
        {
            context.Selections.Add(selection.WithLocation(null));
        }
        else
        {
            var fieldContext = context.Branch(field.Type.NamedType());

            CollectFields(selection.SelectionSet, fieldContext);

            var newSelectionSetNode = new SelectionSetNode(
                null,
                fieldContext.Selections.ToImmutable());

            var newFieldNode = new FieldNode(
                null,
                selection.Name,
                selection.Alias,
                selection.Directives,
                selection.Arguments,
                newSelectionSetNode);

            context.Selections.Add(newFieldNode);
        }
    }

    private static int GetFieldCount(string schemaName, SelectionSetNode selectionSet, ICompositeNamedType type)
    {
        if (type is CompositeComplexType complexType)
        {
            var count = 0;

            foreach (var selection in selectionSet.Selections)
            {
                if (selection is FieldNode fieldNode
                    && complexType.Fields.TryGetField(fieldNode.Name.Value, out var field)
                    && field.Sources.ContainsSchema(schemaName))
                {
                    count++;

                    if (fieldNode.SelectionSet is not null)
                    {
                        count += GetFieldCount(schemaName, fieldNode.SelectionSet, field.Type.NamedType());
                    }
                }
            }

            return count;
        }

        // we will look at unions later
        return 0;
    }

    private sealed class Context(
        string schemaName,
        ICompositeNamedType type,
        Queue<SelectionSet> backlog,
        bool isRoot = false)
    {
        private SelectionSet? _next;

        public string SchemaName => schemaName;

        public ICompositeNamedType Type => type;

        public bool IsRoot => isRoot;

        public ImmutableArray<ISelectionNode>.Builder Selections { get; } =
            ImmutableArray.CreateBuilder<ISelectionNode>();

        public void EnqueueToBacklog(ISelectionNode selection)
        {
            if (_next is null)
            {
                _next = new SelectionSet(SchemaName, Type);
                backlog.Enqueue(_next);
            }

            _next.Selections.Add(selection);
        }

        public Context Branch(ICompositeNamedType type)
            => new(SchemaName, type, backlog);
    }

    private sealed class SelectionSet(string schemaName, ICompositeNamedType type)
    {
        public string SchemaName { get; } = schemaName;

        public ICompositeNamedType Type { get; } = type;

        public List<ISelectionNode> Selections { get; } = new();
    }
}
