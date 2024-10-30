using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public partial class TypeMapperGenerator
{
    /// <summary>
    /// Adds all required deserializers of the given type descriptors properties
    /// </summary>
    protected internal static void AddRequiredMapMethods(
        CSharpSyntaxGeneratorSettings settings,
        ComplexTypeDescriptor typeDescriptor,
        ClassBuilder classBuilder,
        ConstructorBuilder constructorBuilder,
        HashSet<string> processed,
        bool stopAtEntityMappers = false)
    {
        if (typeDescriptor is InterfaceTypeDescriptor interfaceType)
        {
            foreach (var objectTypeDescriptor in interfaceType.ImplementedBy)
            {
                AddRequiredMapMethods(
                    settings,
                    objectTypeDescriptor,
                    classBuilder,
                    constructorBuilder,
                    processed);
            }
        }
        else
        {
            foreach (var property in typeDescriptor.Properties)
            {
                AddMapMethod(
                    settings,
                    property.Type,
                    classBuilder,
                    constructorBuilder,
                    processed);

                if (property.Type.NamedType() is ComplexTypeDescriptor ct &&
                    !ct.IsLeaf() && !stopAtEntityMappers)
                {
                    AddRequiredMapMethods(
                        settings,
                        ct,
                        classBuilder,
                        constructorBuilder,
                        processed);
                }
            }
        }
    }

    private static string MapMethodNameFromTypeName(ITypeDescriptor typeDescriptor)
        => "Map" + BuildMapMethodName(typeDescriptor);

    private static string BuildMapMethodName(
        ITypeDescriptor typeDescriptor,
        bool parentIsList = false)
    {
        return typeDescriptor switch
        {
            ListTypeDescriptor listTypeDescriptor =>
                BuildMapMethodName(listTypeDescriptor.InnerType, true) + "Array",

            ILeafTypeDescriptor leafTypeDescriptor =>
                GetPropertyName(leafTypeDescriptor.Name),

            InterfaceTypeDescriptor
            {
                ImplementedBy.Count: > 1,
                Kind: TypeKind.Entity,
                ParentRuntimeType: { } parentRuntimeType,
            } => parentRuntimeType.Name,

            INamedTypeDescriptor namedTypeDescriptor =>
                namedTypeDescriptor.RuntimeType.Name,

            NonNullTypeDescriptor nonNullTypeDescriptor => parentIsList
                ? BuildMapMethodName(nonNullTypeDescriptor.InnerType) + "NonNullable"
                : "NonNullable" + BuildMapMethodName(nonNullTypeDescriptor.InnerType),

            _ => throw new ArgumentOutOfRangeException(nameof(typeDescriptor)),
        };
    }

    private static void AddMapMethod(
        CSharpSyntaxGeneratorSettings settings,
        ITypeDescriptor typeReference,
        ClassBuilder classBuilder,
        ConstructorBuilder constructorBuilder,
        HashSet<string> processed)
    {
        var methodName = MapMethodNameFromTypeName(typeReference);

        if (!typeReference.IsLeaf() && processed.Add(methodName))
        {
            var methodBuilder = MethodBuilder
                .New()
                .SetAccessModifier(AccessModifier.Private)
                .SetName(methodName)
                .SetReturnType(typeReference.ToTypeReference());

            classBuilder.AddMethod(methodBuilder);

            AddMapMethodBody(
                settings,
                classBuilder,
                constructorBuilder,
                methodBuilder,
                typeReference,
                processed);
        }
    }

    private static CodeBlockBuilder EnsureProperNullability(
        string propertyName,
        bool isNonNullType = false)
    {
        var ifBuilder = IfBuilder
            .New()
            .SetCondition($"{propertyName} is null")
            .AddCode(
                isNonNullType
                    ? CodeLineBuilder.From("return default;")
                    : CodeLineBuilder.From("return null;"));

        return CodeBlockBuilder
            .New()
            .AddCode(ifBuilder)
            .AddEmptyLine();
    }

    protected internal static ICode BuildMapMethodCall(
        CSharpSyntaxGeneratorSettings settings,
        string objectName,
        PropertyDescriptor property,
        bool addNullCheck = false)
    {
        switch (property.Type.Kind)
        {
            case TypeKind.Leaf:
                return CodeInlineBuilder.From($"{objectName}.{property.Name}");

            case TypeKind.AbstractData:
            case TypeKind.EntityOrData:
            case TypeKind.Data:
            case TypeKind.Entity:
                var mapperMethodCall =
                    MethodCallBuilder
                        .Inline()
                        .SetMethodName(MapMethodNameFromTypeName(property.Type));

                ICode argString = CodeInlineBuilder.From($"{objectName}.{property.Name}");
                if (addNullCheck && property.Type.IsNonNull())
                {
                    argString = NullCheckBuilder
                        .Inline()
                        .SetCondition(argString)
                        .SetCode(CodeInlineBuilder.From("default"));
                }

                return mapperMethodCall
                    .AddArgument(argString)
                    .If(settings.IsStoreEnabled(), x => x.AddArgument(_snapshot));

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static void AddMapMethodBody(
        CSharpSyntaxGeneratorSettings settings,
        ClassBuilder classBuilder,
        ConstructorBuilder constructorBuilder,
        MethodBuilder methodBuilder,
        ITypeDescriptor typeDescriptor,
        HashSet<string> processed,
        bool isNonNullable = false)
    {
        switch (typeDescriptor)
        {
            case ListTypeDescriptor listTypeDescriptor:
                AddArrayHandler(
                    settings,
                    classBuilder,
                    constructorBuilder,
                    methodBuilder,
                    listTypeDescriptor,
                    processed,
                    isNonNullable);
                break;

            case ComplexTypeDescriptor { Kind: TypeKind.Leaf, }:
                break;

            case ComplexTypeDescriptor { Kind: TypeKind.EntityOrData, } d:
                AddEntityOrUnionDataHandler(
                    settings,
                    classBuilder,
                    constructorBuilder,
                    methodBuilder,
                    d,
                    processed,
                    isNonNullable);
                break;

            case ComplexTypeDescriptor { Kind: TypeKind.AbstractData, } d:
                AddComplexDataHandler(
                    settings,
                    classBuilder,
                    constructorBuilder,
                    methodBuilder,
                    d,
                    processed,
                    isNonNullable);
                break;

            case ComplexTypeDescriptor { Kind: TypeKind.Data, } d:
                AddDataHandler(
                    settings,
                    classBuilder,
                    constructorBuilder,
                    methodBuilder,
                    d,
                    processed,
                    isNonNullable);
                break;

            case ComplexTypeDescriptor { Kind: TypeKind.Entity, } d:
                AddEntityHandler(
                    classBuilder,
                    constructorBuilder,
                    methodBuilder,
                    d,
                    processed,
                    isNonNullable);
                break;

            case NonNullTypeDescriptor nonNullTypeDescriptor:
                AddMapMethodBody(
                    settings,
                    classBuilder,
                    constructorBuilder,
                    methodBuilder,
                    nonNullTypeDescriptor.InnerType,
                    processed,
                    true);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(typeDescriptor));
        }
    }

    protected internal static void AddMapFragmentMethod(
        ClassBuilder classBuilder,
        ConstructorBuilder constructorBuilder,
        ObjectTypeDescriptor fragmentTypeDescriptor,
        string fragmentInterfaceName,
        string entityTypeName,
        HashSet<string> processed)
    {
        var mapperName = $"{fragmentTypeDescriptor.RuntimeType.Name}Mapper";

        if (!processed.Add(mapperName))
        {
            return;
        }

        const string entity = nameof(entity);
        const string snapshot = nameof(snapshot);
        var methodName = $"Map{fragmentTypeDescriptor.RuntimeType.Name}";
        var fieldName = GetFieldName(mapperName);

        var methodBuilder = MethodBuilder
            .New()
            .SetAccessModifier(AccessModifier.Private)
            .SetName(methodName)
            .AddParameter(ParameterBuilder.New()
                .SetName(entity)
                .SetType(entityTypeName))
            .AddParameter(ParameterBuilder.New()
                .SetName(snapshot)
                .SetType(TypeNames.IEntityStoreSnapshot))
            .SetReturnType($"{fragmentInterfaceName}?");

        classBuilder.AddMethod(methodBuilder);

        methodBuilder.AddCode(
            IfBuilder.New()
                .SetCondition($"!{entity}.Is{fragmentTypeDescriptor.RuntimeType.Name}Fulfilled")
                .AddCode("return null;"));

        var mapperType = TypeNames.IEntityMapper.WithGeneric(
            entityTypeName,
            fragmentTypeDescriptor.RuntimeType.FullName);

        AddConstructorAssignedField(
            mapperType,
            GetFieldName(mapperName),
            GetParameterName(mapperName),
            classBuilder,
            constructorBuilder);

        methodBuilder.AddCode($"return {fieldName}.Map({entity}, {snapshot});");
    }
}
