using StrawberryShake.CodeGeneration.Analyzers.Models;
using Path = HotChocolate.Path;

namespace StrawberryShake.CodeGeneration.Analyzers;

internal class InterfaceTypeSelectionSetAnalyzer : SelectionSetAnalyzer
{
    public override OutputTypeModel Analyze(
        IDocumentAnalyzerContext context,
        FieldSelection fieldSelection,
        SelectionSetVariants selectionVariants)
    {
        var returnTypeFragmentName = FragmentHelper.GetReturnTypeName(fieldSelection);

        if (returnTypeFragmentName is null)
        {
            return AnalyzeWithDefaults(
                context,
                fieldSelection,
                selectionVariants);
        }

        return AnalyzeWithHoistedFragment(
            context,
            fieldSelection,
            selectionVariants,
            returnTypeFragmentName);
    }

    public OutputTypeModel AnalyzeOperation(
        IDocumentAnalyzerContext context,
        SelectionSetVariants selectionSetVariants)
    {
        var rootSelectionPath = Path.Root.Append(context.OperationName);

        var returnTypeFragment =
            FragmentHelper.CreateFragmentNode(
                context.OperationType,
                rootSelectionPath,
                selectionSetVariants.ReturnType);

        returnTypeFragment = FragmentHelper.RewriteForConcreteType(returnTypeFragment);

        var returnType =
            FragmentHelper.CreateInterface(
                context,
                returnTypeFragment,
                rootSelectionPath);

        FragmentHelper.CreateClass(
            context,
            returnTypeFragment,
            selectionSetVariants.ReturnType,
            returnType);

        return returnType;
    }

    private OutputTypeModel AnalyzeWithDefaults(
        IDocumentAnalyzerContext context,
        FieldSelection fieldSelection,
        SelectionSetVariants selectionVariants)
    {
        var returnTypeFragment =
            FragmentHelper.CreateFragmentNode(
                selectionVariants.ReturnType,
                fieldSelection.Path);

        var returnType =
            FragmentHelper.CreateInterface(
                context,
                returnTypeFragment,
                fieldSelection.Path);

        context.RegisterSelectionSet(
            returnType.Type,
            selectionVariants.ReturnType.SyntaxNode,
            returnType.SelectionSet);

        foreach (var selectionSet in selectionVariants.Variants)
        {
            returnTypeFragment = FragmentHelper.CreateFragmentNode(
                selectionSet,
                fieldSelection.Path,
                appendTypeName: true);

            returnTypeFragment = FragmentHelper.RewriteForConcreteType(returnTypeFragment);

            var @interface =
                FragmentHelper.CreateInterface(
                    context,
                    returnTypeFragment,
                    fieldSelection.Path,
                    new[] { returnType, });

            var @class =
                FragmentHelper.CreateClass(
                    context,
                    returnTypeFragment,
                    selectionSet,
                    @interface);

            context.RegisterSelectionSet(
                selectionSet.Type,
                selectionSet.SyntaxNode,
                @class.SelectionSet);
        }

        return returnType;
    }

    private OutputTypeModel AnalyzeWithHoistedFragment(
        IDocumentAnalyzerContext context,
        FieldSelection fieldSelection,
        SelectionSetVariants selectionVariants,
        string fragmentName)
    {
        var returnTypeFragment =
            FragmentHelper.CreateFragmentNode(
                selectionVariants.Variants[0],
                fieldSelection.Path,
                appendTypeName: true);

        returnTypeFragment = FragmentHelper.GetFragment(returnTypeFragment, fragmentName);

        if (returnTypeFragment is null)
        {
            throw ThrowHelper.ReturnFragmentDoesNotExist();
        }

        var returnType =
            FragmentHelper.CreateInterface(context, returnTypeFragment, fieldSelection.Path);

        context.RegisterSelectionSet(
            returnType.Type,
            selectionVariants.ReturnType.SyntaxNode,
            returnType.SelectionSet);

        foreach (var selectionSet in selectionVariants.Variants)
        {
            returnTypeFragment = FragmentHelper.CreateFragmentNode(
                selectionSet,
                fieldSelection.Path,
                appendTypeName: true);

            returnTypeFragment = FragmentHelper.RewriteForConcreteType(returnTypeFragment);

            if (FragmentHelper.GetFragment(returnTypeFragment, fragmentName) is null)
            {
                throw ThrowHelper.FragmentMustBeImplementedByAllTypeFragments();
            }

            var @interface =
                FragmentHelper.CreateInterface(
                    context,
                    returnTypeFragment,
                    fieldSelection.Path,
                    new[] { returnType, });

            var @class =
                FragmentHelper.CreateClass(
                    context,
                    returnTypeFragment,
                    selectionSet,
                    @interface);

            context.RegisterSelectionSet(
                selectionSet.Type,
                selectionSet.SyntaxNode,
                @class.SelectionSet);
        }

        return returnType;
    }
}
