using HotChocolate;
using StrawberryShake.CodeGeneration.Analyzers.Models;

namespace StrawberryShake.CodeGeneration.Analyzers
{
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
            Path rootSelectionPath = Path.Root.Append(context.OperationName);

            FragmentNode returnTypeFragment =
                FragmentHelper.CreateFragmentNode(
                    context.OperationType,
                    rootSelectionPath,
                    selectionSetVariants.ReturnType);

            OutputTypeModel returnType =
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
            FragmentNode returnTypeFragment =
                FragmentHelper.CreateFragmentNode(
                    selectionVariants.ReturnType,
                    fieldSelection.Path);

            OutputTypeModel returnType =
                FragmentHelper.CreateInterface(
                    context,
                    returnTypeFragment,
                    fieldSelection.Path);
            
            context.RegisterSelectionSet(
                returnType.Type, 
                selectionVariants.ReturnType.SyntaxNode,
                returnType.SelectionSet);

            foreach (SelectionSet selectionSet in selectionVariants.Variants)
            {
                returnTypeFragment = FragmentHelper.CreateFragmentNode(
                    selectionSet,
                    fieldSelection.Path,
                    appendTypeName: true);

                OutputTypeModel @interface =
                    FragmentHelper.CreateInterface(
                        context,
                        returnTypeFragment,
                        fieldSelection.Path,
                        new[] { returnType });

                OutputTypeModel @class =
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
            FragmentNode? returnTypeFragment =
                FragmentHelper.CreateFragmentNode(
                    selectionVariants.Variants[0],
                    fieldSelection.Path,
                    appendTypeName: true);

            returnTypeFragment = FragmentHelper.GetFragment(returnTypeFragment, fragmentName);

            if (returnTypeFragment is null)
            {
                // TODO : throw helper
                throw new GraphQLException(
                    "The specified return fragment does not exist.");
            }

            OutputTypeModel returnType =
                FragmentHelper.CreateInterface(context, returnTypeFragment, fieldSelection.Path);

            context.RegisterSelectionSet(
                returnType.Type, 
                selectionVariants.ReturnType.SyntaxNode,
                returnType.SelectionSet);

            foreach (SelectionSet selectionSet in selectionVariants.Variants)
            {
                returnTypeFragment = FragmentHelper.CreateFragmentNode(
                    selectionSet,
                    fieldSelection.Path,
                    appendTypeName: true);

                if (FragmentHelper.GetFragment(returnTypeFragment, fragmentName) is null)
                {
                    // TODO : throw helper
                    throw new GraphQLException(
                        "The specified return fragment must be implement by all type fragments.");
                }

                OutputTypeModel @interface =
                    FragmentHelper.CreateInterface(
                        context,
                        returnTypeFragment,
                        fieldSelection.Path,
                        new[] { returnType });

                OutputTypeModel @class =
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
}
