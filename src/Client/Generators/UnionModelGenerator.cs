using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;
using static StrawberryShake.Generators.Utilities.NameUtils;

namespace StrawberryShake.Generators
{
    internal class UnionModelGenerator
        : SelectionSetModelGenerator<UnionType>
    {
        public override void Generate(
            IModelGeneratorContext context,
            OperationDefinitionNode operation,
            UnionType namedType,
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
            IReadOnlyCollection<SelectionInfo> selections = Normalize(possibleSelections);

            GenerateModels(
                context,
                fieldSelection,
                returnType,
                interfaceDescriptor,
                selections,
                resultParserTypes,
                path);

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

        private void GenerateModels(
            IModelGeneratorContext context,
            FieldNode fieldSelection,
            IFragmentNode returnType,
            IInterfaceDescriptor interfaceDescriptor,
            IReadOnlyCollection<SelectionInfo> selections,
            List<ResultParserTypeDescriptor> resultParserTypes,
            Path path)
        {
            foreach (SelectionInfo selection in selections)
            {
                IFragmentNode modelType = ResolveReturnType(
                    context,
                    selection.Type,
                    fieldSelection,
                    selection);

                var interfaces = new List<IInterfaceDescriptor>();

                foreach (IFragmentNode fragment in
                    ShedNonMatchingFragments(selection.Type, modelType))
                {
                    interfaces.Add(CreateInterfaceModel(context, fragment, path));
                }

                interfaces.Insert(0, interfaceDescriptor);

                NameString typeName = HoistName(selection.Type, modelType);

                string className = context.GetOrCreateName(
                    modelType.Fragment.SelectionSet,
                    GetClassName(typeName));

                var modelClass = new ClassDescriptor(
                    className,
                    context.Namespace,
                    selection.Type,
                    interfaces);

                context.Register(modelClass);
                resultParserTypes.Add(new ResultParserTypeDescriptor(modelClass));
            }
        }
    }
}
