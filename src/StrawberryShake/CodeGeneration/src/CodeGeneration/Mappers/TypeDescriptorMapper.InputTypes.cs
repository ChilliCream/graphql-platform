using HotChocolate.Types;
using HotChocolate.Utilities;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

namespace StrawberryShake.CodeGeneration.Mappers;

public static partial class TypeDescriptorMapper
{
    private static void CollectInputTypes(
        ClientModel model,
        IMapperContext context,
        Dictionary<string, InputTypeDescriptorModel> typeDescriptors)
    {
        foreach (var inputType in model.InputObjectTypes)
        {
            if (!typeDescriptors.TryGetValue(
                    inputType.Name,
                    out var descriptorModel))
            {
                descriptorModel = new InputTypeDescriptorModel(
                    inputType,
                    new InputObjectTypeDescriptor(
                        inputType.Type.Name,
                        new(inputType.Type.Name, context.Namespace),
                        inputType.HasUpload,
                        inputType.Description));

                typeDescriptors.Add(inputType.Name, descriptorModel);
            }
        }
    }

    private static void AddInputTypeProperties(
        Dictionary<string, InputTypeDescriptorModel> typeDescriptors,
        Dictionary<string, INamedTypeDescriptor> leafTypeDescriptors)
    {
        foreach (var typeDescriptorModel in typeDescriptors.Values)
        {
            var properties = new List<PropertyDescriptor>();

            foreach (var field in typeDescriptorModel.Model.Fields)
            {
                INamedTypeDescriptor? fieldType;
                var namedType = field.Type.NamedType();

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
                        field.Field.Name,
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
        Dictionary<string, InputTypeDescriptorModel> typeDescriptors)
    {
        return typeDescriptors.Values
            .First(t => t.Model.Type.Name.EqualsOrdinal(fieldNamedType.Name))
            .Descriptor;
    }
}
