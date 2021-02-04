using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
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
            foreach (OperationModel operation in model.Operations)
            {
                foreach (var inputType in operation.InputObjectTypes)
                {
                    if (!typeDescriptors.TryGetValue(
                        inputType.Name,
                        out InputTypeDescriptorModel descriptorModel))
                    {
                        descriptorModel = new InputTypeDescriptorModel(
                            inputType,
                            new NamedTypeDescriptor(
                                inputType.Name,
                                context.Namespace,
                                false,
                                new List<NameString>(),
                                kind: TypeKind.InputType,
                                graphQLTypeName: inputType.Type.Name));

                        typeDescriptors.Add(
                            inputType.Name,
                            descriptorModel);
                    }
                }
            }
        }

        private static void AddInputTypeProperties(
            ClientModel model,
            IMapperContext context,
            Dictionary<NameString, InputTypeDescriptorModel> typeDescriptors,
            Dictionary<NameString, NamedTypeDescriptor> scalarTypeDescriptors)
        {
            foreach (InputTypeDescriptorModel typeDescriptorModel in typeDescriptors.Values.ToList()
            )
            {
                var properties = new List<PropertyDescriptor>();

                foreach (var field in typeDescriptorModel.TypeModel.Fields)
                {
                    NamedTypeDescriptor? fieldType;
                    INamedType namedType = field.Type.NamedType();

                    if (namedType.IsScalarType())
                    {
                        var scalarType = (ScalarType)namedType;

                        if (!scalarTypeDescriptors.TryGetValue(
                            scalarType.Name,
                            out fieldType))
                        {
                            string[] runtimeTypeName = scalarType.GetRuntimeType().Split('.');

                            fieldType = new NamedTypeDescriptor(
                                runtimeTypeName.Last(),
                                string.Join(
                                    ".",
                                    runtimeTypeName.Take(runtimeTypeName.Length - 1)),
                                false,
                                graphQLTypeName: scalarType.Name,
                                kind: TypeKind.LeafType);

                            scalarTypeDescriptors.Add(
                                scalarType.Name,
                                fieldType);
                        }
                    }
                    else if (namedType.IsEnumType())
                    {
                        var enumTypeModel = model.LeafTypes
                            .OfType<EnumTypeModel>()
                            .First(t => t.Type == namedType);

                        if (!scalarTypeDescriptors.TryGetValue(
                            namedType.Name,
                            out fieldType))
                        {
                            fieldType = new NamedTypeDescriptor(
                                enumTypeModel.Name,
                                context.Namespace,
                                false,
                                graphQLTypeName: namedType.Name,
                                serializationType: enumTypeModel.Type.GetSerializationType(),
                                kind: TypeKind.LeafType,
                                isEnum: true);

                            scalarTypeDescriptors.Add(
                                enumTypeModel.Name,
                                fieldType);
                        }
                    }
                    else
                    {
                        fieldType = GetInputTypeDescriptor(
                            model,
                            field.Type.NamedType(),
                            typeDescriptors);
                    }

                    properties.Add(
                        new PropertyDescriptor(
                            field.Name,
                            BuildFieldType(
                                field.Type,
                                fieldType)));
                }

                typeDescriptorModel.NamedTypeDescriptor.Complete(properties);
            }
        }

        private static NamedTypeDescriptor GetInputTypeDescriptor(
            ClientModel model,
            INamedType fieldNamedType,
            Dictionary<NameString, InputTypeDescriptorModel> typeDescriptors)
        {
            return typeDescriptors.Values
                .First(t => t.TypeModel.Name == fieldNamedType.Name)
                .NamedTypeDescriptor;
        }
    }
}
