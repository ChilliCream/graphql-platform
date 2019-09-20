using System.Collections.Generic;
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
                possibleSelections.ReturnType);

            IInterfaceDescriptor interfaceDescriptor = CreateInterfaceModel(
                context, returnType, path);
            context.Register(fieldSelection, interfaceDescriptor);

            GenerateModels(
                context,
                operation,
                fieldType,
                fieldSelection,
                possibleSelections,
                returnType,
                interfaceDescriptor,
                path);
        }

        private IFragmentNode ResolveReturnType(
            IModelGeneratorContext context,
            INamedType namedType,
            FieldNode field,
            SelectionInfo selection)
        {
            var returnType = new FragmentNode(new Fragment(
                CreateName(namedType, field, GetClassName),
                namedType,
                selection.SelectionSet));

            returnType.Children.AddRange(selection.Fragments);

            return HoistFragment(namedType, returnType);
        }

        private void GenerateModels(
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

            foreach (SelectionInfo selection in Normalize(possibleSelections))
            {
                IFragmentNode modelType = ResolveReturnType(
                    context,
                    selection.Type,
                    fieldSelection,
                    selection);

                IInterfaceDescriptor modelInterface = CreateInterfaceModel(
                    context, modelType, path);

                var modelClass = new ClassDescriptor(
                    GetClassName(modelType.Name),
                    context.Namespace,
                    selection.Type,
                    new[] { interfaceDescriptor, modelInterface });

                context.Register(modelClass);
                resultParserTypes.Add(new ResultParserTypeDescriptor(modelClass));
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
    }
}
