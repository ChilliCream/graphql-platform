using System;
using HotChocolate;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;

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

        public static TypeReferenceBuilder ToBuilder(
            this ITypeDescriptor typeReferenceDescriptor,
            string? nameOverride = null,
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
                    ToBuilder(list.InnerType, nameOverride, actualBuilder.SetListType()),

                EnumTypeDescriptor @enum =>
                    actualBuilder.SetName(nameOverride ?? @enum.RuntimeType.Name),

                ILeafTypeDescriptor leaf =>
                    actualBuilder.SetName(
                        $"{leaf.RuntimeType.Namespace}.{nameOverride ?? leaf.RuntimeType.Name}"),

                INamedTypeDescriptor named =>
                    actualBuilder.SetName(nameOverride ?? named.RuntimeType.Name),

                _ => throw new ArgumentOutOfRangeException(nameof(typeReferenceDescriptor))
            };
        }

        public static TypeReferenceBuilder ToEntityIdBuilder(
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
                    ToEntityIdBuilder(listTypeDescriptor.InnerType, actualBuilder.SetListType()),

                EnumTypeDescriptor @enum =>
                    actualBuilder.SetName(@enum.RuntimeType.Name),

                ILeafTypeDescriptor leaf =>
                    actualBuilder.SetName(leaf.RuntimeType.ToString()),

                ComplexTypeDescriptor { ParentRuntimeType: { } parentRuntimeType } d =>
                    actualBuilder.SetName(
                        $"{parentRuntimeType.Namespace}." +
                        CreateDataTypeName(d.Name!)),

                INamedTypeDescriptor { Kind: TypeKind.DataType } d =>
                    actualBuilder.SetName(
                        $"{d.RuntimeType.Namespace}." + CreateDataTypeName(d.Name!)),

                INamedTypeDescriptor { Kind: TypeKind.EntityType } =>
                    actualBuilder.SetName(TypeNames.EntityId),

                INamedTypeDescriptor d => actualBuilder.SetName(d.RuntimeType.Name),

                _ => throw new ArgumentOutOfRangeException(nameof(typeDescriptor))
            };
        }
    }
}
