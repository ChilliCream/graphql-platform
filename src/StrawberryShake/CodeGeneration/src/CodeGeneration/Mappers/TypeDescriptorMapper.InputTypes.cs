using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.Mappers
{
    public static partial class TypeDescriptorMapper
    {
        private static void CollectInputTypes(
            ClientModel model,
            IMapperContext context,
            Dictionary<NameString, InputTypeDescriptorModel> typeDescriptors)
        {
            foreach (var inputType in model.InputObjectTypes)
            {
                if (!typeDescriptors.TryGetValue(
                    inputType.Name,
                    out InputTypeDescriptorModel descriptorModel))
                {
                    descriptorModel = new InputTypeDescriptorModel(
                        inputType,
                        new InputObjectTypeDescriptor(
                            inputType.Type.Name,
                            new (inputType.Name, context.Namespace),
                            inputType.Description));

                    typeDescriptors.Add(inputType.Name, descriptorModel);
                }
            }
        }

        private static void AddInputTypeProperties(
            Dictionary<NameString, InputTypeDescriptorModel> typeDescriptors,
            Dictionary<NameString, INamedTypeDescriptor> leafTypeDescriptors)
        {
            foreach (InputTypeDescriptorModel typeDescriptorModel in typeDescriptors.Values)
            {
                var properties = new List<PropertyDescriptor>();

                foreach (var field in typeDescriptorModel.Model.Fields)
                {
                    INamedTypeDescriptor? fieldType;
                    INamedType namedType = field.Type.NamedType();

                    if (namedType.IsScalarType() || namedType.IsEnumType())
                    {
                        fieldType = leafTypeDescriptors[namedType.Name];
                    }
                    else
                    {
                        fieldType = GetInputTypeDescriptor(
                            field.Type.NamedType(),
                            typeDescriptors);
                    }

                    properties.Add(
                        new PropertyDescriptor(
                            field.Name,
                            BuildFieldType(
                                field.Type,
                                fieldType),
                            field.Description));
                }

                typeDescriptorModel.Descriptor.CompleteProperties(properties);
            }
        }

        private static INamedTypeDescriptor GetInputTypeDescriptor(
            INamedType fieldNamedType,
            Dictionary<NameString, InputTypeDescriptorModel> typeDescriptors)
        {
            return typeDescriptors.Values
                .First(t => t.Model.Name.Equals(fieldNamedType.Name))
                .Descriptor;
        }
    }
}
