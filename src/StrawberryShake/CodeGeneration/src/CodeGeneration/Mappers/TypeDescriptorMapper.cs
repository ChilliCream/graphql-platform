using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Descriptors;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using StrawberryShake.CodeGeneration.Utilities;
using static System.StringComparer;
using TypeKind = StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors.TypeKind;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;

namespace StrawberryShake.CodeGeneration.Mappers;

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
        var typeDescriptors = new Dictionary<string, TypeDescriptorModel>(Ordinal);
        var inputTypeDescriptors = new Dictionary<string, InputTypeDescriptorModel>(Ordinal);
        var leafTypeDescriptors = new Dictionary<string, INamedTypeDescriptor>(Ordinal);

        // first we create type descriptor for the leaf types ...
        CollectLeafTypes(model, context, leafTypeDescriptors);

        // after that we collect all the output types that we have ...
        CollectTypes(model, context, typeDescriptors);

        // with these two completed we can create the properties for all output
        // types and complete them since we know now all the property types.
        AddProperties(model, typeDescriptors, leafTypeDescriptors);

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
        Dictionary<string, TypeDescriptorModel> typeDescriptors)
    {
        foreach (var operation in model.Operations)
        {
            foreach (var outputType in
                operation.GetImplementations(operation.ResultType))
            {
                RegisterType(
                    model,
                    context,
                    typeDescriptors,
                    outputType,
                    TypeKind.Result);
            }

            foreach (var outputType in
                operation.OutputTypes.Where(t => !t.IsInterface))
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
                TypeKind.Result,
                operation);

            foreach (var outputType in
                operation.OutputTypes.Where(t => t.IsInterface))
            {
                if (!typeDescriptors.TryGetValue(
                        outputType.Name,
                        out var _))
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

    private static void RegisterType(
        ClientModel model,
        IMapperContext context,
        Dictionary<string, TypeDescriptorModel> typeDescriptors,
        OutputTypeModel outputType,
        TypeKind? kind = null,
        OperationModel? operationModel = null)
    {
        if (typeDescriptors.TryGetValue(
            outputType.Name,
            out var descriptorModel))
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
                operationModel,
                kind);
        }
        else
        {
            descriptorModel = CreateObjectTypeModel(
                model,
                context,
                outputType,
                kind);
        }

        typeDescriptors.Add(outputType.Name, descriptorModel);
    }

    private static void ExtractTypeKindAndParentRuntimeType(
        IMapperContext context,
        OutputTypeModel outputType,
        IEnumerable<ObjectTypeDescriptor>? implementedBy,
        out TypeKind fallbackKind,
        out RuntimeTypeInfo? parentRuntimeType)
    {
        parentRuntimeType = null;
        string? parentRuntimeTypeName = null;

        if (outputType.Type.IsEntity())
        {
            fallbackKind = TypeKind.Entity;
        }
        else
        {
            if (outputType.Type.IsAbstractType())
            {
                switch (outputType.Type)
                {
                    // if the output type is a union of which all types are entities,
                    // then the union is an also considered an entity.
                    case UnionType typeA when typeA.Types.Values.All(t => t.IsEntity()):
                        fallbackKind = TypeKind.Entity;
                        break;

                    case UnionType typeB when typeB.Types.Values.Any(t => t.IsEntity()):
                        fallbackKind = TypeKind.EntityOrData;
                        parentRuntimeTypeName = GetInterfaceName(outputType.Type.Name);
                        break;
                    case InterfaceType when implementedBy is not null &&
                        implementedBy.Any(t => t.IsEntity()):
                        fallbackKind = TypeKind.EntityOrData;
                        parentRuntimeTypeName = GetInterfaceName(outputType.Type.Name);
                        break;

                    default:
                        fallbackKind = TypeKind.AbstractData;
                        parentRuntimeTypeName = GetInterfaceName(outputType.Type.Name);
                        break;
                }
            }
            else
            {
                var mostAbstractTypeModel = outputType;
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
                        ? TypeKind.Data
                        : TypeKind.AbstractData;
            }
        }

        if (parentRuntimeTypeName is not null)
        {
            parentRuntimeType =
                new RuntimeTypeInfo(
                    CreateDataTypeName(parentRuntimeTypeName),
                    CreateStateNamespace(context.Namespace));
        }

        if (outputType.IsFragment)
        {
            fallbackKind = TypeKind.Fragment;
        }
    }

    private static IReadOnlyList<string> ExtractImplementsBy(
        ClientModel clientModel,
        IMapperContext context,
        OutputTypeModel outputType,
        TypeKind kind)
    {
        if (kind == TypeKind.Result && outputType.Implements.Count > 0)
        {
            var runtimeType =
                ExtractRuntimeType(
                    clientModel,
                    context,
                    outputType.Implements.Single(),
                    kind);

            return new[] { runtimeType.Name, };
        }

        return outputType.Implements
            .Select(t => t.Name)
            .ToList();
    }

    private static IReadOnlyList<DeferredFragmentDescriptor> ExtractDeferredFragments(
        OutputTypeModel outputType)
        => outputType.Deferred.Values
            .Select(t => new DeferredFragmentDescriptor(
                t.Label,
                t.Interface.Name,
                t.Class.Name))
            .ToArray();

    private static RuntimeTypeInfo ExtractRuntimeType(
        ClientModel clientModel,
        IMapperContext context,
        OutputTypeModel outputType,
        TypeKind kind)
    {
        RuntimeTypeInfo runtimeType;

        if (kind == TypeKind.Result)
        {
            var resultTypeName = CreateResultRootTypeName(outputType.Name);
            if (clientModel.OutputTypes.Any(t => t.Name.EqualsOrdinal(resultTypeName)))
            {
                resultTypeName = CreateResultRootTypeName(outputType.Name, outputType.Type);
                if (clientModel.OutputTypes.Any(t => t.Name.EqualsOrdinal(resultTypeName)))
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
        Dictionary<string, TypeDescriptorModel> typeDescriptors,
        OutputTypeModel outputType,
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
            implementedBy,
            out var extractedKind,
            out var parentRuntimeType);

        var typeKind = kind ?? extractedKind;

        return new TypeDescriptorModel(
            outputType,
            new InterfaceTypeDescriptor(
                outputType.Type.Name,
                typeKind,
                ExtractRuntimeType(model, context, outputType, typeKind),
                implementedBy,
                ExtractImplementsBy(model, context, outputType, typeKind),
                ExtractDeferredFragments(outputType),
                outputType.Description,
                parentRuntimeType));
    }

    private static TypeDescriptorModel CreateObjectTypeModel(
        ClientModel model,
        IMapperContext context,
        OutputTypeModel outputType,
        TypeKind? kind = null)
    {
        ExtractTypeKindAndParentRuntimeType(
            context,
            outputType,
            Array.Empty<ObjectTypeDescriptor>(),
            out var extractedKind,
            out var parentRuntimeType);

        var typeKind = kind ?? extractedKind;

        return new TypeDescriptorModel(
            outputType,
            new ObjectTypeDescriptor(
                outputType.Type.Name,
                typeKind,
                ExtractRuntimeType(model, context, outputType, typeKind),
                ExtractImplementsBy(model, context, outputType, typeKind),
                ExtractDeferredFragments(outputType),
                outputType.Description,
                parentRuntimeType));
    }

    private static void CollectClassesThatImplementInterface(
        OperationModel operation,
        OutputTypeModel outputType,
        Dictionary<string, TypeDescriptorModel> typeDescriptors,
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
        Dictionary<string, TypeDescriptorModel> typeDescriptors,
        Dictionary<string, INamedTypeDescriptor> leafTypeDescriptors)
    {
        foreach (var typeDescriptorModel in typeDescriptors.Values.ToList())
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
                    fieldType = GetFieldTypeDescriptor(
                        model,
                        field.SyntaxNode,
                        field.Type.NamedType(),
                        typeDescriptors);
                }

                properties.Add(
                    new PropertyDescriptor(
                        field.Name,
                        field.ResponseName,
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
        Dictionary<string, INamedTypeDescriptor> leafTypeDescriptors)
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
                        : model.Schema.GetOrCreateTypeInfo(enumTypeModel.UnderlyingType),
                    enumTypeModel.Values
                        .Select(
                            t => new EnumValueDescriptor(t.Name, t.Value.Name, t.Description))
                        .ToList(),
                    enumTypeModel.Description);

                leafTypeDescriptors.Add(leafType.Type.Name, descriptor);
            }
            else
            {
                descriptor = new ScalarTypeDescriptor(
                    leafType.Name,
                    model.Schema.GetOrCreateTypeInfo(leafType.RuntimeType),
                    model.Schema.GetOrCreateTypeInfo(leafType.SerializationType));

                leafTypeDescriptors.Add(leafType.Type.Name, descriptor);
            }
        }
    }

    private static INamedTypeDescriptor GetFieldTypeDescriptor(
        ClientModel model,
        FieldNode fieldSyntax,
        INamedType fieldNamedType,
        Dictionary<string, TypeDescriptorModel> typeDescriptors)
    {
        foreach (var operation in model.Operations)
        {
            if (operation.TryGetFieldResultType(
                    fieldSyntax,
                    fieldNamedType,
                    out var fieldType))
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
