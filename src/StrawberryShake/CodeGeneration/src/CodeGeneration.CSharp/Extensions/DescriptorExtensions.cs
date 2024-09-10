using Microsoft.CodeAnalysis.CSharp.Syntax;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace StrawberryShake.CodeGeneration.CSharp.Extensions;

public static class DescriptorExtensions
{
    public static string ExtractMapperName(this INamedTypeDescriptor descriptor)
        => descriptor.Kind == TypeKind.Entity
            ? CreateEntityMapperName(descriptor.RuntimeType.Name, descriptor.Name)
            : CreateDataMapperName(descriptor.RuntimeType.Name, descriptor.Name);

    public static RuntimeTypeInfo ExtractType(this INamedTypeDescriptor descriptor)
    {
        return descriptor.IsEntity()
            ? CreateEntityType(descriptor.Name, descriptor.RuntimeType.NamespaceWithoutGlobal)
            : new(descriptor.Name, descriptor.RuntimeType.NamespaceWithoutGlobal);
    }

    public static TypeSyntax ToTypeSyntax(
        this ITypeDescriptor typeReferenceDescriptor,
        TypeReferenceBuilder? builder = null) =>
        ParseTypeName(typeReferenceDescriptor.ToTypeReference(builder).ToString());

    public static TypeReferenceBuilder ToTypeReference(
        this ITypeDescriptor typeReferenceDescriptor,
        TypeReferenceBuilder? builder = null,
        bool nonNull = false)
    {
        var actualBuilder = builder ?? TypeReferenceBuilder.New();

        if (typeReferenceDescriptor is NonNullTypeDescriptor n)
        {
            typeReferenceDescriptor = n.InnerType;
        }
        else if (!nonNull)
        {
            actualBuilder.SetIsNullable(true);
        }

        return typeReferenceDescriptor switch
        {
            ListTypeDescriptor list =>
                ToTypeReference(list.InnerType, actualBuilder.SetListType()),

            EnumTypeDescriptor @enum =>
                actualBuilder.SetName(@enum.RuntimeType.ToString()),

            ILeafTypeDescriptor leaf =>
                actualBuilder.SetName(leaf.RuntimeType.ToString()),

            INamedTypeDescriptor named =>
                actualBuilder.SetName(named.RuntimeType.ToString()),

            _ => throw new ArgumentOutOfRangeException(nameof(typeReferenceDescriptor)),
        };
    }

    public static TypeSyntax ToStateTypeSyntax(
        this ITypeDescriptor typeDescriptor,
        TypeReferenceBuilder? builder = null) =>
        ParseTypeName(typeDescriptor.ToStateTypeReference(builder).ToString());

    public static TypeReferenceBuilder ToStateTypeReference(
        this ITypeDescriptor typeDescriptor,
        TypeReferenceBuilder? builder = null)
    {
        var actualBuilder = builder ?? TypeReferenceBuilder.New();

        if (typeDescriptor is NonNullTypeDescriptor n)
        {
            typeDescriptor = n.InnerType;
        }
        else
        {
            actualBuilder.SetIsNullable(true);
        }

        return typeDescriptor switch
        {
            ListTypeDescriptor listTypeDescriptor =>
                ToStateTypeReference(listTypeDescriptor.InnerType, actualBuilder.SetListType()),

            EnumTypeDescriptor @enum =>
                actualBuilder.SetName(@enum.RuntimeType.ToString()),

            ILeafTypeDescriptor leaf =>
                actualBuilder.SetName(leaf.RuntimeType.ToString()),

            INamedTypeDescriptor { Kind: TypeKind.EntityOrData, } =>
                actualBuilder.SetName(TypeNames.EntityIdOrData),

            ComplexTypeDescriptor { ParentRuntimeType: { } parentRuntimeType, } =>
                actualBuilder.SetName(parentRuntimeType.ToString()),

            INamedTypeDescriptor { Kind: TypeKind.Data, } d =>
                actualBuilder.SetName(d.RuntimeType.ToString()),

            INamedTypeDescriptor { Kind: TypeKind.Entity, } =>
                actualBuilder.SetName(TypeNames.EntityId),

            INamedTypeDescriptor d => actualBuilder.SetName(d.RuntimeType.ToString()),

            _ => throw new ArgumentOutOfRangeException(nameof(typeDescriptor)),
        };
    }
}
