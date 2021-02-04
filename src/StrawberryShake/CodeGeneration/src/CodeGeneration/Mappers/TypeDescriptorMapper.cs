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
        public static void Map(
            ClientModel model,
            IMapperContext context)
        {
            var typeDescriptors = new Dictionary<NameString, TypeDescriptorModel>();
            var inputTypeDescriptors = new Dictionary<NameString, InputTypeDescriptorModel>();
            var scalarTypeDescriptors = new Dictionary<NameString, NamedTypeDescriptor>();

            CollectTypes(
                model,
                context,
                typeDescriptors);
            AddProperties(
                model,
                context,
                typeDescriptors,
                scalarTypeDescriptors);

            CollectInputTypes(
                model,
                context,
                inputTypeDescriptors);

            AddInputTypeProperties(
                model,
                context,
                inputTypeDescriptors,
                scalarTypeDescriptors);

            foreach (TypeDescriptorModel descriptorModel in typeDescriptors.Values)
            {
                context.Register(
                    descriptorModel.NamedTypeDescriptor.Name,
                    descriptorModel.NamedTypeDescriptor);
            }

            foreach (InputTypeDescriptorModel descriptorModel in inputTypeDescriptors.Values)
            {
                context.Register(
                    descriptorModel.NamedTypeDescriptor.Name,
                    descriptorModel.NamedTypeDescriptor);
            }

            foreach (NamedTypeDescriptor typeDescriptor in scalarTypeDescriptors.Values)
            {
                context.Register(
                    typeDescriptor.Name,
                    typeDescriptor);
            }


        }

        private static void CollectTypes(
            ClientModel model,
            IMapperContext context,
            Dictionary<NameString, TypeDescriptorModel> typeDescriptors)
        {
            foreach (OperationModel operation in model.Operations)
            {
                foreach (OutputTypeModel outputType in operation.GetImplementations(
                    operation.ResultType))
                {
                    RegisterType(
                        model,
                        context,
                        typeDescriptors,
                        outputType,
                        TypeKind.ResultType);
                }

                foreach (var outputType in operation.OutputTypes.Where(t => !t.IsInterface))
                {
                    RegisterType(
                        model,
                        context,
                        typeDescriptors,
                        outputType);
                }

                RegisterType(
                    model,
                    context,
                    typeDescriptors,
                    operation.ResultType,
                    TypeKind.ResultType,
                    operation);

                foreach (var outputType in operation.OutputTypes.Where(t => t.IsInterface))
                {
                    if (!typeDescriptors.TryGetValue(
                        outputType.Name,
                        out TypeDescriptorModel descriptorModel))
                    {
                        RegisterType(
                            model,
                            context,
                            typeDescriptors,
                            outputType,
                            operationModel: operation);
                    }
                }
            }
        }

        private static void RegisterType(ClientModel model,
            IMapperContext context,
            Dictionary<NameString, TypeDescriptorModel> typeDescriptors,
            OutputTypeModel outputType,
            TypeKind? kind = null,
            OperationModel? operationModel = null)
        {
            if (!typeDescriptors.TryGetValue(
                outputType.Name,
                out TypeDescriptorModel descriptorModel))
            {
                descriptorModel = new TypeDescriptorModel(
                    outputType,
                    new NamedTypeDescriptor(
                        outputType.Name,
                        context.Namespace,
                        outputType.IsInterface,
                        outputType.Implements.Select(t => t.Name).ToList(),
                        kind: kind ?? (outputType.Type.IsEntity()
                            ? TypeKind.EntityType
                            : TypeKind.DataType),
                        graphQLTypeName: outputType.Type.Name,
                        implementedBy: operationModel?.GetImplementations(outputType)
                            .Select(t => typeDescriptors[t.Name])
                            .Select(t => t.NamedTypeDescriptor)
                            .ToList() ?? new List<NamedTypeDescriptor>()));

                typeDescriptors.Add(
                    outputType.Name,
                    descriptorModel);
            }
        }

        private static void AddProperties(
            ClientModel model,
            IMapperContext context,
            Dictionary<NameString, TypeDescriptorModel> typeDescriptors,
            Dictionary<NameString, NamedTypeDescriptor> scalarTypeDescriptors)
        {
            foreach (TypeDescriptorModel typeDescriptorModel in typeDescriptors.Values.ToList())
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
                        fieldType = GetFieldTypeDescriptor(
                            model,
                            field.SyntaxNode,
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

        private static NamedTypeDescriptor GetFieldTypeDescriptor(
            ClientModel model,
            FieldNode fieldSyntax,
            INamedType fieldNamedType,
            Dictionary<NameString, TypeDescriptorModel> typeDescriptors)
        {
            foreach (var operation in model.Operations)
            {
                if (operation.TryGetFieldResultType(
                    fieldSyntax,
                    fieldNamedType,
                    out OutputTypeModel? fieldType))
                {
                    return typeDescriptors.Values
                        .First(t => t.TypeModel == fieldType)
                        .NamedTypeDescriptor;
                }
            }

            throw new InvalidOperationException(
                "Could not find an output type for the specified field syntax.");
        }

        private static ITypeDescriptor BuildFieldType(
            this IType original,
            NamedTypeDescriptor namedTypeDescriptor)
        {
            if (original is NonNullType nnt)
            {
                return new NonNullTypeDescriptor(
                    BuildFieldType(
                        nnt.Type,
                        namedTypeDescriptor));
            }

            if (original is ListType lt)
            {
                return new ListTypeDescriptor(
                    BuildFieldType(
                        lt.ElementType,
                        namedTypeDescriptor));
            }

            if (original is INamedType)
            {
                return namedTypeDescriptor;
            }

            throw new NotSupportedException();
        }

        private readonly struct TypeDescriptorModel
        {
            public TypeDescriptorModel(OutputTypeModel typeModel,
                NamedTypeDescriptor namedTypeDescriptor)
            {
                TypeModel = typeModel;
                NamedTypeDescriptor = namedTypeDescriptor;
            }

            public OutputTypeModel TypeModel { get; }

            public NamedTypeDescriptor NamedTypeDescriptor { get; }
        }

        private readonly struct InputTypeDescriptorModel
        {
            public InputTypeDescriptorModel(InputObjectTypeModel typeModel,
                NamedTypeDescriptor namedTypeDescriptor)
            {
                TypeModel = typeModel;
                NamedTypeDescriptor = namedTypeDescriptor;
            }

            public InputObjectTypeModel TypeModel { get; }

            public NamedTypeDescriptor NamedTypeDescriptor { get; }
        }
    }
}
