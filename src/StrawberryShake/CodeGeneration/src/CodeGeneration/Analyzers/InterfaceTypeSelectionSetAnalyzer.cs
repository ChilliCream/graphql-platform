using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Utilities;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    internal class InterfaceTypeSelectionSetAnalyzer
        : SelectionSetAnalyzerBase<InterfaceType>
    {
        public override void Analyze(
            IDocumentAnalyzerContext context,
            OperationDefinitionNode operation,
            FieldNode fieldSelection,
            PossibleSelections possibleSelections,
            IType fieldType,
            InterfaceType namedType,
            Path path)
        {
            IFragmentNode returnTypeFragment =
                ResolveReturnType(
                    namedType,
                    fieldSelection,
                    possibleSelections.ReturnType);

            ComplexOutputTypeModel returnType =
                CreateInterfaceModel(
                    context,
                    returnTypeFragment,
                    path);

            CreateClassModels(
                context,
                operation,
                fieldSelection,
                possibleSelections,
                returnTypeFragment,
                returnType,
                path);
        }

        private void CreateClassModels(
            IDocumentAnalyzerContext context,
            OperationDefinitionNode operation,
            FieldNode fieldSelection,
            PossibleSelections possibleSelections,
            IFragmentNode returnTypeFragment,
            ComplexOutputTypeModel returnType,
            Path path)
        {
            IReadOnlyCollection<SelectionInfo> selections = possibleSelections.Variants;
            var resultParserTypes = new List<ComplexOutputTypeModel>();

            if (selections.Count == 1)
            {
                ComplexOutputTypeModel modelType = CreateClassModel(
                    context,
                    returnTypeFragment,
                    returnType,
                    selections.Single());

                CreateFieldParserModel(
                    context,
                    operation,
                    fieldSelection,
                    path,
                    returnType,
                    modelType);
            }
            else
            {
                IReadOnlyList<ComplexOutputTypeModel> modelTypes =
                    CreateClassModels(
                        context,
                        returnTypeFragment,
                        returnType,
                        fieldSelection,
                        selections,
                        path);

                CreateFieldParserModel(
                    context,
                    operation,
                    fieldSelection,
                    path,
                    returnType,
                    modelTypes);
            }
        }
    }
}
