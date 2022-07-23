using System.Reflection.Metadata;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

internal sealed class ExecutionPlanBuilder
{
    private readonly Metadata.Schema _schema;

    public ExecutionPlanBuilder(Metadata.Schema schema)
    {
        _schema = schema;
    }

    public IReadOnlyList<DocumentNode> Build(QueryPlanContext context)
    {
        var list = new List<DocumentNode>();

        foreach (var step in context.Steps)
        {
            if (step is SelectionExecutionStep executionStep)
            {
                list.Add(CreateRequestDocument(context, executionStep));
            }
        }

        return list;
    }

    private DocumentNode CreateRequestDocument(
        QueryPlanContext context,
        SelectionExecutionStep executionStep)
    {
        var rootSelectionSetNode = CreateRootSelectionSetNode(context, executionStep);

        if (executionStep.Resolver is not null)
        {
            var rootResolver = executionStep.Resolver.CreateSelection(
                executionStep.Variables,
                rootSelectionSetNode);

            rootSelectionSetNode = new SelectionSetNode(new[] { rootResolver });
        }

        var operationDefinitionNode = new OperationDefinitionNode(
            null,
            context.CreateRemoteOperationName(),
            OperationType.Query,
            context.Exports.CreateVariableDefinitions(executionStep.Variables.Values),
            Array.Empty<DirectiveNode>(),
            rootSelectionSetNode);

        return new DocumentNode(new[] { operationDefinitionNode });
    }

    private SelectionSetNode CreateRootSelectionSetNode(
        QueryPlanContext context,
        SelectionExecutionStep executionStep)
    {
        var selectionNodes = new List<ISelectionNode>();

        // create
        foreach (var rootSelection in executionStep.RootSelections)
        {
            ISelectionNode selectionNode;
            var field = executionStep.DeclaringType.Fields[rootSelection.Selection.Field.Name];

            if (rootSelection.Resolver is null)
            {
                selectionNode = CreateSelectionNode(
                    context,
                    executionStep,
                    rootSelection.Selection,
                    field);
            }
            else
            {

                SelectionSetNode? selectionSetNode = null;

                if (rootSelection.Selection.SelectionSet is not null)
                {
                    selectionSetNode = CreateSelectionSetNode(
                        context,
                        executionStep,
                        rootSelection.Selection);
                }

                selectionNode = rootSelection.Resolver.CreateSelection(
                    executionStep.Variables,
                    selectionSetNode);
            }

            selectionNodes.Add(selectionNode);
        }

        // append exports that were required by other execution steps.
        foreach (var selection in context.Exports.GetExportSelections(executionStep))
        {
            selectionNodes.Add(selection);
        }

        return new SelectionSetNode(selectionNodes);
    }

    private ISelectionNode CreateSelectionNode(
        QueryPlanContext context,
        SelectionExecutionStep executionStep,
        ISelection selection,
        ObjectField field)
    {
        SelectionSetNode? selectionSetNode = null;

        if (selection.SelectionSet is not null)
        {
            selectionSetNode = CreateSelectionSetNode(context, executionStep, selection);
        }

        var binding = field.Bindings[executionStep.SchemaName];

        var alias = !selection.ResponseName.Equals(binding.Name)
            ? new NameNode(selection.ResponseName)
            : null;

        return new FieldNode(
            null,
            new(binding.Name),
            alias,
            null,
            Array.Empty<DirectiveNode>(),
            Array.Empty<ArgumentNode>(),
            selectionSetNode);
    }

    private SelectionSetNode CreateSelectionSetNode(
        QueryPlanContext context,
        SelectionExecutionStep executionStep,
        ISelection parentSelection)
    {
        // TODO : we need to spec inline fragments or a simple selectionsSet depending on pt
        var selectionNodes = new List<ISelectionNode>();
        var possibleTypes = context.Operation.GetPossibleTypes(parentSelection);

        foreach (var possibleType in possibleTypes)
        {
            var typeContext = _schema.GetType<ObjectType>(possibleType.Name);
            var selectionSet = context.Operation.GetSelectionSet(parentSelection, possibleType);

            foreach (var selection in selectionSet.Selections)
            {
                if (executionStep.AllSelections.Contains(selection))
                {
                    var field = typeContext.Fields[selection.Field.Name];
                    var selectionNode = CreateSelectionNode(
                        context,
                        executionStep,
                        selection,
                        field);
                    selectionNodes.Add(selectionNode);
                }
            }
        }

        return new SelectionSetNode(selectionNodes);
    }
}
