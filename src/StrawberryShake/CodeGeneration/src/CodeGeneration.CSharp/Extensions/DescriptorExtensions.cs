using System;
using HotChocolate;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp.Extensions
{
    public static class DescriptorExtensions
    {
        public static NameString ExtractMapperName(this INamedTypeDescriptor descriptor)
        {
            return descriptor.Kind == TypeKind.EntityType
                ? CreateEntityMapperName(
                    descriptor.RuntimeType.Name,
                    descriptor.Name)
                : CreateDataMapperName(
                    descriptor.RuntimeType.Name,
                    descriptor.Name);
        }

        public static NameString ExtractTypeName(this INamedTypeDescriptor descriptor)
        {
            return descriptor.IsEntityType()
                ? CreateEntityTypeName(descriptor.Name)
                : descriptor.Name;
        }

        public static TypeReferenceBuilder ToTypeReference(
            this ITypeDescriptor typeReferenceDescriptor,
            TypeReferenceBuilder? builder = null)
        {
            TypeReferenceBuilder actualBuilder = builder ?? TypeReferenceBuilder.New();

            if (typeReferenceDescriptor is NonNullTypeDescriptor n)
            {
                typeReferenceDescriptor = n.InnerType;
            }
            else
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

                _ => throw new ArgumentOutOfRangeException(nameof(typeReferenceDescriptor))
            };
        }

        public static TypeReferenceBuilder ToStateTypeReference(
            this ITypeDescriptor typeDescriptor,
            TypeReferenceBuilder? builder = null)
        {
            TypeReferenceBuilder actualBuilder = builder ?? TypeReferenceBuilder.New();

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

                ComplexTypeDescriptor { ParentRuntimeType: { } parentRuntimeType }  =>
                    actualBuilder.SetName(parentRuntimeType.ToString()),

                INamedTypeDescriptor { Kind: TypeKind.DataType } d =>
                    actualBuilder.SetName(d.RuntimeType.ToString()),

                INamedTypeDescriptor { Kind: TypeKind.EntityType } =>
                    actualBuilder.SetName(TypeNames.EntityId),

                INamedTypeDescriptor d => actualBuilder.SetName(d.RuntimeType.ToString()),

                _ => throw new ArgumentOutOfRangeException(nameof(typeDescriptor))
            };
        }
    }
}
