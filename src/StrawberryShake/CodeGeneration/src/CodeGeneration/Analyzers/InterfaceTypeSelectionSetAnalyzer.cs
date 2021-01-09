using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models2;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    internal class InterfaceTypeSelectionSetAnalyzer
        : SelectionSetAnalyzer
    {
        public override OutputTypeModel Analyze(
           IDocumentAnalyzerContext context,
           FieldSelection fieldSelection,
           SelectionSetVariants selectionVariants)
        {
            FragmentNode returnTypeFragment =
                ResolveReturnType(
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
                returnTypeFragment,
                returnType);

            return null;
        }

        private void CreateClassModels(
            IDocumentAnalyzerContext context,
            FieldSelection fieldSelection,
            SelectionSetVariants selectionSetVariants,
            FragmentNode returnTypeFragment,
            OutputTypeModel returnType)
        {
            foreach (SelectionSet modelSelectionSet in selectionSetVariants.Variants)
            {
                FragmentNode modelTypeFragment =
                    ResolveReturnType(
                        fieldSelection,
                        modelSelectionSet,
                        true);

                var x = CreateClassModel(
                    context,
                    fieldSelection,
                    modelTypeFragment,
                    returnType);
            }
        }
    }
}
