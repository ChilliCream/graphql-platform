using StrawberryShake.CodeGeneration.Analyzers.Models2;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    internal class ObjectTypeSelectionSetAnalyzer : SelectionSetAnalyzer
    {
        public override OutputTypeModel Analyze(
            IDocumentAnalyzerContext context,
            FieldSelection fieldSelection,
            SelectionSetVariants selectionSetVariants)
        {
            FragmentNode returnTypeFragment =
                ResolveReturnType(
                    fieldSelection,
                    selectionSetVariants.ReturnType);

            OutputTypeModel returnType =
                CreateInterfaceModel(
                    context,
                    returnTypeFragment,
                    fieldSelection.Path);

            CreateClassModel(
                context,
                fieldSelection,
                returnTypeFragment,
                returnType);

            return returnType;
        }
    }
}
