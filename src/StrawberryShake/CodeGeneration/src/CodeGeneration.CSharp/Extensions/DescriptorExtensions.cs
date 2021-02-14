using System;
using HotChocolate;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp.Extensions
{
    public static class DescriptorExtensions
    {
        public static NameString ExtractMapperName(this NamedTypeDescriptor descriptor)
        {
            return descriptor.Kind == TypeKind.EntityType
                ? EntityMapperNameFromGraphQLTypeName(
                    descriptor.Name,
                    descriptor.GraphQLTypeName!)
                : DataMapperNameFromGraphQLTypeName(
                    descriptor.Name,
                    descriptor.GraphQLTypeName!);
        }

        public static NameString ExtractTypeName(this NamedTypeDescriptor descriptor)
        {
            return descriptor.IsEntityType()
                ? EntityTypeNameFromGraphQLTypeName(descriptor.GraphQLTypeName!)
                : descriptor.Name;
        }

        public static TypeReferenceBuilder ToBuilder(
            this ITypeDescriptor typeReferenceDescriptor,
            string? nameOverride = null,
            TypeReferenceBuilder? builder = null,
            bool isNonNull = false)
        {
            var actualBuilder = builder ?? TypeReferenceBuilder.New();
            switch (typeReferenceDescriptor)
            {
                case ListTypeDescriptor listTypeDescriptor:
                    actualBuilder.SetIsNullable(!isNonNull);
                    actualBuilder.SetListType();
                    ToBuilder(
                        listTypeDescriptor.InnerType,
                        nameOverride,
                        actualBuilder);
                    break;
                case NamedTypeDescriptor namedTypeDescriptor:
                    actualBuilder.SetIsNullable(!isNonNull);
                    if (namedTypeDescriptor.IsLeafType() && !namedTypeDescriptor.IsEnum)
                    {
                        actualBuilder.SetName(
                            $"{namedTypeDescriptor.Namespace}." +
                            (nameOverride ?? namedTypeDescriptor.Name));
                    }
                    else
                    {
                        actualBuilder.SetName(nameOverride ?? namedTypeDescriptor.Name);
                    }

                    break;
                case NonNullTypeDescriptor nonNullTypeDescriptor:
                    ToBuilder(
                        nonNullTypeDescriptor.InnerType,
                        nameOverride,
                        actualBuilder,
                        true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(typeReferenceDescriptor));
            }

            return actualBuilder;
        }

        public static TypeReferenceBuilder ToEntityIdBuilder(
            this ITypeDescriptor typeDescriptor,
            TypeReferenceBuilder? builder = null,
            bool isNonNull = false)
        {
            TypeReferenceBuilder actualBuilder = builder ?? TypeReferenceBuilder.New();
            switch (typeDescriptor)
            {
                case ListTypeDescriptor listTypeDescriptor:
                    actualBuilder.SetIsNullable(!isNonNull);
                    actualBuilder.SetListType();
                    ToEntityIdBuilder(
                        listTypeDescriptor.InnerType,
                        actualBuilder);
                    break;
                case NamedTypeDescriptor namedTypeDescriptor:
                    actualBuilder.SetIsNullable(!isNonNull);
                    if (namedTypeDescriptor.IsLeafType() && !namedTypeDescriptor.IsEnum)
                    {
                        actualBuilder.SetName(
                            $"{namedTypeDescriptor.Namespace}.{namedTypeDescriptor.Name}");
                    }
                    else if (namedTypeDescriptor.IsDataType())
                    {
                        actualBuilder.SetName(
                            namedTypeDescriptor.Kind == TypeKind.ComplexDataType
                                ? $"global::{namedTypeDescriptor.Namespace}.State.I" +
                                  DataTypeNameFromTypeName(
                                      namedTypeDescriptor.ComplexDataTypeParent)
                                : $"global::{namedTypeDescriptor.Namespace}.State.{DataTypeNameFromTypeName(namedTypeDescriptor.GraphQLTypeName)}");
                    }
                    else if (namedTypeDescriptor.IsEntityType())
                    {
                        actualBuilder.SetName(TypeNames.EntityId);
                    }
                    else
                    {
                        actualBuilder.SetName(typeDescriptor.Name);
                    }

                    break;
                case NonNullTypeDescriptor nonNullTypeDescriptor:
                    ToEntityIdBuilder(
                        nonNullTypeDescriptor.InnerType,
                        actualBuilder,
                        true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(typeDescriptor));
            }

            return actualBuilder;
        }
    }
}
