using HotChocolate;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

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
                    context,
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

        public OutputTypeModel AnalyzeOperation(
            IDocumentAnalyzerContext context,
            SelectionSetVariants selectionSetVariants)
        {
            Path rootSelectionPath = Path.Root.Append(context.OperationName);

            FragmentNode returnTypeFragment =
                ResolveReturnType(
                    context.OperationType,
                    rootSelectionPath,
                    selectionSetVariants.ReturnType);

            OutputTypeModel returnType =
                CreateInterfaceModel(
                    context,
                    returnTypeFragment,
                    rootSelectionPath);

            CreateClassModel(
                context,
                context.OperationDefinition.SelectionSet,
                rootSelectionPath,
                returnTypeFragment,
                returnType);

            return returnType;
        }

        private FragmentNode ResolveReturnType(
            INamedType namedType,
            Path selectionPath,
            SelectionSet selectionSet,
            bool appendTypeName = false)
        {
            string name = CreateName(selectionPath, GetClassName);

            if (appendTypeName)
            {
                name += "_" + selectionSet.Type.NamedType().Name;
            }

            var returnType = new FragmentNode(new Fragment(
                    name,
                    FragmentKind.Structure,
                    namedType,
                    selectionSet.SyntaxNode),
                selectionSet.FragmentNodes);

            return HoistFragment(returnType, namedType);
        }
    }
}
