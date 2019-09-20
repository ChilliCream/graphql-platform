using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;
using static StrawberryShake.Generators.Utilities.NameUtils;

namespace StrawberryShake.Generators
{
    internal class InterfaceModelGenerator
        : SelectionSetModelGenerator<InterfaceType>
    {
        public override void Generate(
            IModelGeneratorContext context,
            OperationDefinitionNode operation,
            InterfaceType namedType,
            IType fieldType,
            FieldNode fieldSelection,
            PossibleSelections possibleSelections,
            Path path)
        {
            IFragmentNode returnType = ResolveReturnType(
                context,
                namedType,
                fieldSelection,
                possibleSelections,
                path);

            IInterfaceDescriptor interfaceDescriptor = CreateInterfaceModel(
                context, returnType, path);

            GeneratePossibleTypeModel(
                context,
                operation,
                fieldType,
                fieldSelection,
                possibleSelections,
                returnType,
                interfaceDescriptor,
                path);

            context.Register(interfaceDescriptor);
        }

        private IFragmentNode ResolveReturnType(
            IModelGeneratorContext context,
            InterfaceType namedType,
            FieldNode fieldSelection,
            PossibleSelections possibleSelections,
            Path path)
        {
            var returnType = new FragmentNode(new Fragment(
                CreateName(namedType, fieldSelection, GetClassName),
                namedType,
                possibleSelections.ReturnType.SelectionSet));

            returnType.Children.AddRange(
                possibleSelections.ReturnType.Fragments);

            return HoistFragment(namedType, returnType);
        }

        private void GeneratePossibleTypeModel(
            IModelGeneratorContext context,
            OperationDefinitionNode operation,
            IType fieldType,
            FieldNode fieldSelection,
            PossibleSelections possibleSelections,
            IFragmentNode returnType,
            IInterfaceDescriptor interfaceDescriptor,
            Path path)
        {
            var resultParserTypes = new List<ResultParserTypeDescriptor>();

            foreach (SelectionInfo possibleSelection in
                Normalize(possibleSelections))
            {
                GeneratePossibleTypeModel(
                    context,
                    possibleSelection,
                    returnType,
                    resultParserTypes,
                    path);
            }

            context.Register(
                new ResultParserMethodDescriptor(
                    GetPathName(path),
                    operation,
                    fieldType,
                    fieldSelection,
                    path,
                    interfaceDescriptor,
                    resultParserTypes));
        }


        private void GeneratePossibleTypeModel(
            IModelGeneratorContext context,
            SelectionInfo selectionInfo,
            IFragmentNode returnType,
            ICollection<ResultParserTypeDescriptor> resultParser,
            Path path)
        {
            string className;
            IReadOnlyList<IFragmentNode> fragments;

            IFragmentNode modelType = new FragmentNode(new Fragment(
                selectionInfo.Type.Name,
                selectionInfo.Type,
                selectionInfo.SelectionSet));

            modelType = HoistFragment(
                selectionInfo.Type,
                modelType);

            IInterfaceDescriptor modelInterface = CreateInterfaceModel(
                context, modelType, path);

            var modelClass = new ClassDescriptor(
                GetClassName(modelType.Name),
                context.Namespace,
                selectionInfo.Type,
                modelInterface);

            context.Register(modelInterface);
            context.Register(modelClass);

            resultParser.Add(new ResultParserTypeDescriptor(modelClass));
        }
    }
}
