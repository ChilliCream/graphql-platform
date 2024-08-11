using System.Collections.Immutable;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed class RequestPlanner(CompositeSchema schema)
{
    public DocumentNode RewriteDocument(DocumentNode document, string? operationName)
    {
        var backlog = new Queue<SelectionSet>();
        var operation = document.GetOperation(operationName);
        var operationType = schema.GetOperationType(operation.Operation);
        var context = new Context("a", operationType, backlog, isRoot: true);

        RewriteFields(operation.SelectionSet, context);

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

    private void PlanRootFields(
        SelectionSetNode selectionSet,
        CompositeObjectType operationType,
        Queue<SelectionSet> backlog)
    {
        foreach (var selection in GetRootFields())
        {
            switch (selection)
            {
                case FieldNode fieldSelection:
                    operationType.Fields[fieldSelection.Name.Value].Sources.OrderBy(t => t.BaseCost)
                    break;
            }
        }
    }



    private void RewriteFields(SelectionSetNode selectionSet, Context context)
    {
        if (context.Type is CompositeComplexType complexType)
        {
            foreach (var selection in selectionSet.Selections)
            {
                switch (selection)
                {
                    case FieldNode field:
                        RewriteField(field, complexType, context);
                        break;
                }
            }
        }
    }

    private void RewriteField(FieldNode selection, CompositeComplexType complexType, Context context)
    {
        if (!complexType.Fields.TryGetField(selection.Name.Value, out var field)
            || !field.Sources.ContainsSchema(context.SchemaName))
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

            RewriteFields(selection.SelectionSet, fieldContext);

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

    private IEnumerable<string> GetRootFields(SelectionSetNode selectionSet)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode field:
                    yield return field.Name.Value;
                    break;
            }
        }
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
