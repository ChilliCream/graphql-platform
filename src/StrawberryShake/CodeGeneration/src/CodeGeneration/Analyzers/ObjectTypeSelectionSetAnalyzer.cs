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
            SelectionVariants selectionVariants,
            IType fieldType,
            ObjectType namedType,
            Path path)
        {
            IFragmentNode returnTypeFragment =
                ResolveReturnType(
                    namedType,
                    fieldSelection,
                    selectionVariants.ReturnType);

            OutputTypeModel returnType =
                CreateInterfaceModel(
                    context,
                    returnTypeFragment,
                    path);

            OutputTypeModel modelType =
                CreateClassModel(
                    context,
                    returnTypeFragment,
                    returnType,
                    selectionVariants.ReturnType,
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
