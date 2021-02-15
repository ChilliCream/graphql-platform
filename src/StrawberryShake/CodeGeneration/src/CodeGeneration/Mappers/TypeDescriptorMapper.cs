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
            foreach (var (nameString, namedTypeDescriptor) in
                CollectTypeDescriptors(
                    model,
                    context))
            {
                context.Register(
                    nameString,
                    namedTypeDescriptor);
            }
        }

        public static IEnumerable<(NameString, NamedTypeDescriptor )> CollectTypeDescriptors(
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
                yield return (
                    descriptorModel.NamedTypeDescriptor.Name,
                    descriptorModel.NamedTypeDescriptor);
            }

            foreach (InputTypeDescriptorModel descriptorModel in inputTypeDescriptors.Values)
            {
                yield return (
                    descriptorModel.NamedTypeDescriptor.Name,
                    descriptorModel.NamedTypeDescriptor);
            }

            foreach (NamedTypeDescriptor typeDescriptor in scalarTypeDescriptors.Values)
            {
                yield return (
                    typeDescriptor.Name + "_" + typeDescriptor.GraphQLTypeName!.Value,
                    typeDescriptor);
            }
        }

        private static void CollectTypes(
            ClientModel model,
            IMapperContext context,
            Dictionary<NameString, TypeDescriptorModel> typeDescriptors)
        {
            var unionTypes = model.Schema.Types.OfType<UnionType>().ToList();

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
                        unionTypes,
                        TypeKind.ResultType);
                }

                foreach (var outputType in operation.OutputTypes.Where(t => !t.IsInterface))
                {
                    RegisterType(
                        model,
                        context,
                        typeDescriptors,
                        outputType,
                        unionTypes);
                }

                RegisterType(
                    model,
                    context,
                    typeDescriptors,
                    operation.ResultType,
                    unionTypes,
                    TypeKind.ResultType,
                    operation);

                foreach (var outputType in operation.OutputTypes.Where(t => t.IsInterface))
                {
                    if (!typeDescriptors.TryGetValue(
                        outputType.Name,
                        out TypeDescriptorModel _))
                    {
                        RegisterType(
                            model,
                            context,
                            typeDescriptors,
                            outputType,
                            unionTypes,
                            operationModel: operation);
                    }
                }
            }
        }

        private static void RegisterType(
            ClientModel model,
            IMapperContext context,
            Dictionary<NameString, TypeDescriptorModel> typeDescriptors,
            OutputTypeModel outputType,
            List<UnionType> unionTypes,
            TypeKind? kind = null,
            OperationModel? operationModel = null)
        {
            if (!typeDescriptors.TryGetValue(
                outputType.Name,
                out TypeDescriptorModel descriptorModel))
            {
                IReadOnlyList<NamedTypeDescriptor> implementedBy =
                    Array.Empty<NamedTypeDescriptor>();

                if (operationModel is not null && outputType.IsInterface)
                {
                    var classes = new HashSet<NamedTypeDescriptor>();
                    CollectClassesThatImplementInterface(
                        operationModel,
                        outputType,
                        typeDescriptors,
                        classes);
                    implementedBy = classes.ToList();
                }

                TypeKind fallbackKind;
                string? complexDataTypeParent = null;
                if (outputType.Type.IsEntity())
                {
                    fallbackKind = TypeKind.EntityType;
                }
                else
                {
                    if (outputType.Type.IsAbstractType())
                    {
                        fallbackKind = TypeKind.ComplexDataType;
                        complexDataTypeParent = outputType.Type.Name;
                    }
                    else
                    {
                        OutputTypeModel? mostAbstractTypeModel = outputType;
                        while (mostAbstractTypeModel.Implements.Count > 0)
                        {
                            mostAbstractTypeModel = mostAbstractTypeModel.Implements[0];
                        }

                        complexDataTypeParent = mostAbstractTypeModel.Type.Name;

                        if (complexDataTypeParent == outputType.Type.Name)
                        {
                            fallbackKind = TypeKind.DataType;
                        }
                        else
                        {
                            fallbackKind = TypeKind.ComplexDataType;
                        }
                    }
                }

                descriptorModel = new TypeDescriptorModel(
                    outputType,
                    new NamedTypeDescriptor(
                        outputType.Name,
                        context.Namespace,
                        outputType.IsInterface,
                        outputType.Implements.Select(t => t.Name).ToList(),
                        kind: kind ?? fallbackKind,
                        graphQLTypeName: outputType.Type.Name,
                        implementedBy: implementedBy,
                        complexDataTypeParent: complexDataTypeParent));

                typeDescriptors.Add(
                    outputType.Name,
                    descriptorModel);
            }
        }

        private static void CollectClassesThatImplementInterface(
            OperationModel operation,
            OutputTypeModel outputType,
            Dictionary<NameString, TypeDescriptorModel> typeDescriptors,
            HashSet<NamedTypeDescriptor> classes)
        {
            foreach (var type in operation.GetImplementations(outputType))
            {
                if (type.IsInterface)
                {
                    CollectClassesThatImplementInterface(
                        operation,
                        type,
                        typeDescriptors,
                        classes);
                }
                else
                {
                    classes.Add(typeDescriptors[type.Name].NamedTypeDescriptor);
                }
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
            public TypeDescriptorModel(
                OutputTypeModel typeModel,
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
            public InputTypeDescriptorModel(
                InputObjectTypeModel typeModel,
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
