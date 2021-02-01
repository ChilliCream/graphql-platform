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
            FragmentNode returnTypeFragment =
                ResolveReturnType(
                    context,
                    fieldSelection,
                    selectionVariants.ReturnType);

            OutputTypeModel returnType =
                CreateInterfaceModel(
                    context,
                    returnTypeFragment,
                    fieldSelection.Path);

            CreateClassModels(
                context,
                fieldSelection,
                selectionVariants,
                returnType);

            return returnType;
        }

        private void CreateClassModels(
            IDocumentAnalyzerContext context,
            FieldSelection fieldSelection,
            SelectionSetVariants selectionSetVariants,
            OutputTypeModel returnType)
        {
            if (selectionSetVariants.Variants.Count == 0)
            {
                FragmentNode modelTypeFragment =
                    ResolveReturnType(
                        context,
                        fieldSelection,
                        selectionSetVariants.ReturnType);

                CreateClassModel(
                    context,
                    fieldSelection,
                    modelTypeFragment,
                    returnType);
            }
            else
            {
                foreach (SelectionSet modelSelectionSet in selectionSetVariants.Variants)
                {
                    FragmentNode modelTypeFragment =
                        ResolveReturnType(
                            context,
                            fieldSelection,
                            modelSelectionSet,
                            true);

                    CreateClassModel(
                        context,
                        fieldSelection,
                        modelTypeFragment,
                        returnType);
                }
            }
        }
    }
}
