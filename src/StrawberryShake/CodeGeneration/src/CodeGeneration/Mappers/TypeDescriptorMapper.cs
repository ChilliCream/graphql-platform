using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Descriptors;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using TypeKind = StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors.TypeKind;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;

namespace StrawberryShake.CodeGeneration.Mappers
{
    public static partial class TypeDescriptorMapper
    {
        public static void Map(ClientModel model, IMapperContext context)
        {
            context.Register(CollectTypeDescriptors(model, context));
        }

        private static IEnumerable<INamedTypeDescriptor> CollectTypeDescriptors(
            ClientModel model,
            IMapperContext context)
        {
            var typeDescriptors = new Dictionary<NameString, TypeDescriptorModel>();
            var inputTypeDescriptors = new Dictionary<NameString, InputTypeDescriptorModel>();
            var leafTypeDescriptors = new Dictionary<NameString, INamedTypeDescriptor>();

            // first we create type descriptor for the leaf types ...
            CollectLeafTypes(model, context, leafTypeDescriptors);

            // after that we collect all the output types that we have ...
            CollectTypes(model, context, typeDescriptors);

            // with these two completed we can create the properties for all output
            // types and complete them since we know now all the property types.
            AddProperties(model, context, typeDescriptors, leafTypeDescriptors);

            // last but not least we will collect all the input object types ...
            CollectInputTypes(model, context, inputTypeDescriptors);

            // and in a second step complete the input object types since we now know all
            // the possible property types.
            AddInputTypeProperties(inputTypeDescriptors, leafTypeDescriptors);

            return typeDescriptors.Values
                .Select(t => t.Descriptor)
                .Concat(inputTypeDescriptors.Values
                    .Select(t => t.Descriptor)
                    .Concat(leafTypeDescriptors.Values));
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
            if (typeDescriptors.TryGetValue(
                outputType.Name,
                out TypeDescriptorModel descriptorModel))
            {
                return;
            }

            if (operationModel is not null && outputType.IsInterface)
            {
                descriptorModel = CreateInterfaceTypeModel(
                    model,
                    context,
                    typeDescriptors,
                    outputType,
                    unionTypes,
                    descriptorModel,
                    operationModel,
                    kind);
            }
            else
            {
                descriptorModel = CreateObjectTypeModel(
                    model,
                    context,
                    typeDescriptors,
                    outputType,
                    unionTypes,
                    descriptorModel,
                    kind);

            }

            typeDescriptors.Add(outputType.Name, descriptorModel);
        }

        private static void ExtractTypeKindAndParentRuntimeType(
            IMapperContext context,
            OutputTypeModel outputType,
            out TypeKind fallbackKind,
            out RuntimeTypeInfo? parentRuntimeType)
        {
            parentRuntimeType = null;
            string? parentRuntimeTypeName = null;

            if (outputType.Type.IsEntity())
            {
                fallbackKind = TypeKind.EntityType;
            }
            else
            {
                if (outputType.Type.IsAbstractType())
                {
                    fallbackKind = TypeKind.ComplexDataType;
                    parentRuntimeTypeName = GetInterfaceName(outputType.Type.Name);
                }
                else
                {
                    OutputTypeModel? mostAbstractTypeModel = outputType;
                    while (mostAbstractTypeModel.Implements.Count > 0)
                    {
                        mostAbstractTypeModel = mostAbstractTypeModel.Implements[0];
                    }

                    parentRuntimeTypeName =
                        mostAbstractTypeModel.Type.IsAbstractType()
                            ? GetInterfaceName(mostAbstractTypeModel.Type.Name)
                            : mostAbstractTypeModel.Type.Name;

                    fallbackKind =
                        parentRuntimeTypeName == outputType.Type.Name
                            ? TypeKind.DataType
                            : TypeKind.ComplexDataType;
                }
            }

            if (parentRuntimeTypeName is not null)
            {
                parentRuntimeType =
                    new RuntimeTypeInfo(
                        NamingConventions.CreateDataTypeName(parentRuntimeTypeName),
                        CreateStateNamespace(context.Namespace));
            }
        }

        private static IReadOnlyList<NameString> ExtractImplementsBy(
            ClientModel clientModel,
            IMapperContext context,
            OutputTypeModel outputType,
            TypeKind kind)
        {
            if (kind == TypeKind.ResultType && outputType.Implements.Count > 0)
            {
                RuntimeTypeInfo runtimeType =
                    ExtractRuntimeType(
                        clientModel, 
                        context, 
                        outputType.Implements.Single(), 
                        kind);

                return new NameString[] { runtimeType.Name };
            }

            return outputType.Implements
                .Select(t => t.Name)
                .ToList();
        }

        private static RuntimeTypeInfo ExtractRuntimeType(
            ClientModel clientModel,
            IMapperContext context,
            OutputTypeModel outputType,
            TypeKind kind)
        {
            RuntimeTypeInfo runtimeType;

            if (kind == TypeKind.ResultType)
            {
                NameString resultTypeName = CreateResultRootTypeName(outputType.Name);
                if (clientModel.OutputTypes.Any(t => t.Name.Equals(resultTypeName)))
                {
                    resultTypeName = CreateResultRootTypeName(outputType.Name, outputType.Type);
                    if (clientModel.OutputTypes.Any(t => t.Name.Equals(resultTypeName)))
                    {
                        throw ThrowHelper.ResultTypeNameCollision(resultTypeName);
                    }
                }

                runtimeType = new RuntimeTypeInfo(resultTypeName, context.Namespace);
                context.Register(outputType.Name, kind, runtimeType);
                return runtimeType;
            }

            runtimeType = new RuntimeTypeInfo(outputType.Name, context.Namespace);

            if (!context.Register(outputType.Name, kind, runtimeType))
            {
               throw ThrowHelper.TypeNameCollision(runtimeType.Name);
            }

            return runtimeType;
        }

        private static TypeDescriptorModel CreateInterfaceTypeModel(
            ClientModel model,
            IMapperContext context,
            Dictionary<NameString, TypeDescriptorModel> typeDescriptors,
            OutputTypeModel outputType,
            List<UnionType> unionTypes,
            TypeDescriptorModel descriptorModel,
            OperationModel operationModel,
            TypeKind? kind = null)
        {
            var implementedBy = new HashSet<ObjectTypeDescriptor>();

            CollectClassesThatImplementInterface(
                operationModel,
                outputType,
                typeDescriptors,
                implementedBy);

            ExtractTypeKindAndParentRuntimeType(
                context,
                outputType,
                out var extractedKind,
                out var parentRuntimeType);

            var typeKind = kind ?? extractedKind;

            IReadOnlyList<NameString> implements =
                ExtractImplementsBy(model, context, outputType, typeKind);
            RuntimeTypeInfo runtimeType = ExtractRuntimeType(model, context, outputType, typeKind);

            return new TypeDescriptorModel(
                outputType,
                new InterfaceTypeDescriptor(
                    outputType.Type.Name,
                    typeKind,
                    runtimeType,
                    implementedBy,
                    implements,
                    outputType.Description,
                    parentRuntimeType));
        }

        private static TypeDescriptorModel CreateObjectTypeModel(
            ClientModel model,
            IMapperContext context,
            Dictionary<NameString, TypeDescriptorModel> typeDescriptors,
            OutputTypeModel outputType,
            List<UnionType> unionTypes,
            TypeDescriptorModel descriptorModel,
            TypeKind? kind = null)
        {
            ExtractTypeKindAndParentRuntimeType(
                context,
                outputType,
                out var extractedKind,
                out var parentRuntimeType);

            var typeKind = kind ?? extractedKind;

            IReadOnlyList<NameString> implements =
                ExtractImplementsBy(model, context, outputType, typeKind);
            RuntimeTypeInfo runtimeType = ExtractRuntimeType(model, context, outputType, typeKind);

            return new TypeDescriptorModel(
                outputType,
                new ObjectTypeDescriptor(
                    outputType.Type.Name,
                    typeKind,
                    runtimeType,
                    implements,
                    outputType.Description,
                    parentRuntimeType));
        }


        private static void CollectClassesThatImplementInterface(
            OperationModel operation,
            OutputTypeModel outputType,
            Dictionary<NameString, TypeDescriptorModel> typeDescriptors,
            HashSet<ObjectTypeDescriptor> implementations)
        {
            foreach (var type in operation.GetImplementations(outputType))
            {
                if (type.IsInterface)
                {
                    CollectClassesThatImplementInterface(
                        operation,
                        type,
                        typeDescriptors,
                        implementations);
                }
                else
                {
                    implementations.Add(
                        (ObjectTypeDescriptor)typeDescriptors[type.Name].Descriptor);
                }
            }
        }

        private static void AddProperties(
            ClientModel model,
            IMapperContext context,
            Dictionary<NameString, TypeDescriptorModel> typeDescriptors,
            Dictionary<NameString, INamedTypeDescriptor> leafTypeDescriptors)
        {
            foreach (TypeDescriptorModel typeDescriptorModel in typeDescriptors.Values.ToList())
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
                                fieldType),
                            field.Description));
                }

                typeDescriptorModel.Descriptor.CompleteProperties(properties);
            }
        }

        private static void CollectLeafTypes(
            ClientModel model,
            IMapperContext context,
            Dictionary<NameString, INamedTypeDescriptor> leafTypeDescriptors)
        {
            foreach (var leafType in model.LeafTypes)
            {
                INamedTypeDescriptor descriptor;

                if (leafType is EnumTypeModel enumTypeModel)
                {
                    descriptor = new EnumTypeDescriptor(
                        leafType.Name,
                        new(enumTypeModel.Name, context.Namespace, isValueType: true),
                        enumTypeModel.UnderlyingType is null
                            ? null
                            : TypeInfos.From(enumTypeModel.UnderlyingType),
                        enumTypeModel.Values
                            .Select(
                                t => new EnumValueDescriptor(t.Name, t.Value.Name, t.Description))
                            .ToList(),
                        enumTypeModel.Description);

                    leafTypeDescriptors.Add(leafType.Name, descriptor);
                }
                else
                {
                    descriptor = new ScalarTypeDescriptor(
                        leafType.Name,
                        TypeInfos.From(leafType.RuntimeType),
                        TypeInfos.From(leafType.SerializationType));

                    leafTypeDescriptors.Add(leafType.Name, descriptor);
                }
            }
        }

        private static INamedTypeDescriptor GetFieldTypeDescriptor(
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
                        .First(t => t.Model == fieldType)
                        .Descriptor;
                }
            }

            throw new InvalidOperationException(
                "Could not find an output type for the specified field syntax.");
        }

        private static ITypeDescriptor BuildFieldType(
            this IType original,
            INamedTypeDescriptor namedTypeDescriptor)
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
                ComplexTypeDescriptor namedTypeDescriptor)
            {
                Model = typeModel;
                Descriptor = namedTypeDescriptor;
            }

            public OutputTypeModel Model { get; }

            public ComplexTypeDescriptor Descriptor { get; }
        }

        private readonly struct InputTypeDescriptorModel
        {
            public InputTypeDescriptorModel(
                InputObjectTypeModel model,
                InputObjectTypeDescriptor descriptor)
            {
                Model = model;
                Descriptor = descriptor;
            }

            public InputObjectTypeModel Model { get; }

            public InputObjectTypeDescriptor Descriptor { get; }
        }
    }
}
