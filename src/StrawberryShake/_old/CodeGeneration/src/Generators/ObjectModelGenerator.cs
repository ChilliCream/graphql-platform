using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;
using static StrawberryShake.Generators.Utilities.NameUtils;

namespace StrawberryShake.Generators
{
    internal class ObjectModelGenerator
        : SelectionSetModelGenerator<ObjectType>
    {
        public override ICodeDescriptor Generate(
            IModelGeneratorContext context,
            OperationDefinitionNode operation,
            ObjectType namedType,
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

            var resultParserTypes = new List<ResultParserTypeDescriptor>();

            CreateClassModel(
                context,
                returnType,
                interfaceDescriptor,
                possibleSelections.ReturnType,
                resultParserTypes);

            context.Register(
                new ResultParserMethodDescriptor(
                    GetPathName(path),
                    operation,
                    fieldType,
                    fieldSelection,
                    path,
                    interfaceDescriptor,
                    resultParserTypes));

            return interfaceDescriptor;
        }
    }
}
