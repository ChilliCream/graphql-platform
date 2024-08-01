using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Utilities;

namespace StrawberryShake.CodeGeneration.Analyzers;

public partial class DocumentAnalyzer
{
    private static readonly InterfaceTypeSelectionSetAnalyzer _selectionAnalyzer = new();

    private static OperationModel CreateOperationModel(
        IDocumentAnalyzerContext context)
    {
        CollectEnumTypes(context);
        CollectInputObjectTypes(context);

        return new(
            context.OperationName,
            context.OperationType,
            QueryDocumentRewriter.Rewrite(context.Document, context.Schema),
            context.OperationDefinition.Operation,
            CreateOperationArguments(context),
            GetResultType(context),
            context.TypeModels.OfType<LeafTypeModel>().ToList(),
            context.TypeModels.OfType<InputObjectTypeModel>().ToList(),
            context.TypeModels.OfType<OutputTypeModel>().ToList(),
            context.SelectionSets);
    }

    private static OutputTypeModel GetResultType(
        IDocumentAnalyzerContext context)
    {
        Queue<FieldSelection> backlog = new();
        var root = VisitOperationSelectionSet(context, backlog);

        while (backlog.Any())
        {
            var current = backlog.Dequeue();
            var namedType = current.Field.Type.NamedType();

            if (namedType.IsLeafType())
            {
                context.RegisterType(namedType);
            }
            else
            {
                VisitFieldSelectionSet(context, current, backlog);
            }
        }

        return root;
    }

    private static OutputTypeModel VisitOperationSelectionSet(
        IDocumentAnalyzerContext context,
        Queue<FieldSelection> backlog)
    {
        var selectionSetVariants =
            context.CollectFields(
                context.OperationDefinition.SelectionSet,
                context.OperationType,
                context.RootPath);

        EnqueueFields(selectionSetVariants, backlog);

        return _selectionAnalyzer.AnalyzeOperation(
            context,
            selectionSetVariants);
    }

    private static void VisitFieldSelectionSet(
        IDocumentAnalyzerContext context,
        FieldSelection fieldSelection,
        Queue<FieldSelection> backlog)
    {
        var namedType = (INamedOutputType)fieldSelection.Field.Type.NamedType();

        var selectionSetVariants =
            context.CollectFields(
                fieldSelection.SyntaxNode.SelectionSet!,
                namedType,
                fieldSelection.Path);

        EnqueueFields(selectionSetVariants, backlog);

        if (namedType is UnionType or InterfaceType)
        {
            _selectionAnalyzer.Analyze(
                context,
                fieldSelection,
                selectionSetVariants);
        }
        else if (namedType is ObjectType)
        {
            _selectionAnalyzer.Analyze(
                context,
                fieldSelection,
                selectionSetVariants);
        }
    }

    private static IReadOnlyList<ArgumentModel> CreateOperationArguments(
        IDocumentAnalyzerContext context)
    {
        var arguments = new List<ArgumentModel>();

        foreach (var variableDefinition in
                 context.OperationDefinition.VariableDefinitions)
        {
            var namedInputType = context.Schema.GetType<INamedInputType>(
                variableDefinition.Type.NamedType().Name.Value);

            arguments.Add(new ArgumentModel(
                variableDefinition.Variable.Name.Value,
                (IInputType)variableDefinition.Type.ToType(namedInputType),
                variableDefinition,
                variableDefinition.DefaultValue));
        }

        return arguments;
    }

    private static void EnqueueFields(
        SelectionSetVariants selectionSetVariants,
        Queue<FieldSelection> backlog)
    {
        if (selectionSetVariants.Variants.Count == 0)
        {
            foreach (var fieldSelection in selectionSetVariants.ReturnType.Fields)
            {
                backlog.Enqueue(fieldSelection);
            }
        }
        else
        {
            foreach (var selectionSet in selectionSetVariants.Variants)
            {
                foreach (var fieldSelection in selectionSet.Fields)
                {
                    backlog.Enqueue(fieldSelection);
                }
            }
        }
    }
}
