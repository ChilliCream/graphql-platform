using System.Collections.Generic;
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
        var resultType = GetResultType(context);
        var arguments = CreateOperationArguments(context);

        var leafTypes = new List<LeafTypeModel>();
        var inputTypes = new List<InputObjectTypeModel>();
        var outputTypes = new List<OutputTypeModel>();

        foreach (ITypeModel typeModel in context.TypeModels)
        {
            switch (typeModel)
            {
                case LeafTypeModel m:
                    leafTypes.Add(m);
                    break;
                case InputObjectTypeModel m:
                    inputTypes.Add(m);
                    break;
                case OutputTypeModel m:
                    outputTypes.Add(m);
                    break;
            }
        }

        return new(
            context.OperationName,
            context.OperationType,
            QueryDocumentRewriter.Rewrite(context.Document, context.Schema),
            context.OperationDefinition.Operation,
            arguments,
            resultType,
            leafTypes,
            inputTypes,
            outputTypes,
            context.SelectionSets);
    }

    private static OutputTypeModel GetResultType(
        IDocumentAnalyzerContext context)
    {
        Queue<FieldSelection> backlog = new();
        OutputTypeModel root = VisitOperationSelectionSet(context, backlog);

        while (backlog.Count > 0)
        {
            FieldSelection current = backlog.Dequeue();
            INamedType namedType = current.Field.Type.NamedType();

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
        SelectionSetVariants selectionSetVariants =
            context.CollectFields(
                context.OperationDefinition.SelectionSet,
                context.OperationType,
                context.RootPath);

        EnqueueFields(selectionSetVariants, backlog);

        return InterfaceTypeSelectionSetAnalyzer.AnalyzeOperation(
            context,
            selectionSetVariants);
    }

    private static void VisitFieldSelectionSet(
        IDocumentAnalyzerContext context,
        FieldSelection fieldSelection,
        Queue<FieldSelection> backlog)
    {
        var namedType = (INamedOutputType)fieldSelection.Field.Type.NamedType();

        SelectionSetVariants selectionSetVariants =
            context.CollectFields(
                fieldSelection.SyntaxNode.SelectionSet!,
                namedType,
                fieldSelection.Path);

        EnqueueFields(selectionSetVariants, backlog);

        if (namedType.IsCompositeType())
        {
            _selectionAnalyzer.Analyze(context, fieldSelection, selectionSetVariants);
        }
    }

    private static IReadOnlyList<ArgumentModel> CreateOperationArguments(
        IDocumentAnalyzerContext context)
    {
        var arguments = new List<ArgumentModel>();

        foreach (VariableDefinitionNode variableDefinition in
            context.OperationDefinition.VariableDefinitions)
        {
            INamedInputType namedInputType = context.Schema.GetType<INamedInputType>(
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
            foreach (FieldSelection fieldSelection in selectionSetVariants.ReturnType.Fields)
            {
                backlog.Enqueue(fieldSelection);
            }
        }
        else
        {
            foreach (SelectionSet selectionSet in selectionSetVariants.Variants)
            {
                foreach (FieldSelection fieldSelection in selectionSet.Fields)
                {
                    backlog.Enqueue(fieldSelection);
                }
            }
        }
    }
}