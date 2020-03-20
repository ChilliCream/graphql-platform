using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Utilities;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    internal class ObjectTypeSelectionSetAnalyzer
        : SelectionSetAnalyzerBase<ObjectType>
    {
        public override void Analyze(
            IDocumentAnalyzerContext context,
            OperationDefinitionNode operation,
            FieldNode fieldSelection,
            PossibleSelections possibleSelections,
            IType fieldType,
            ObjectType namedType,
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

            ComplexOutputTypeModel modelType =
                CreateClassModel(
                    context,
                    returnTypeFragment,
                    returnType,
                    possibleSelections.ReturnType,
                    path);

            CreateFieldParserModel(
                context,
                operation,
                fieldSelection,
                path,
                returnType,
                fieldType,
                modelType);
        }
    }
}
