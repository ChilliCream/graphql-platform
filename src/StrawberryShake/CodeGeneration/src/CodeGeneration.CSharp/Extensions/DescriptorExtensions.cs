using System;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp.Extensions
{
    public static class DescriptorExtensions
    {
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
                    actualBuilder.SetName(nameOverride ?? namedTypeDescriptor.Name);
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
            this ITypeDescriptor typeReferenceDescriptor,
            TypeReferenceBuilder? builder = null,
            bool isNonNull = false)
        {
            var actualBuilder = builder ?? TypeReferenceBuilder.New();
            switch (typeReferenceDescriptor)
            {
                case ListTypeDescriptor listTypeDescriptor:
                    actualBuilder.SetListType();
                    ToEntityIdBuilder(
                        listTypeDescriptor.InnerType,
                        actualBuilder);
                    break;
                case NamedTypeDescriptor namedTypeDescriptor:
                    actualBuilder.SetIsNullable(!isNonNull);
                    actualBuilder.SetName(
                        typeReferenceDescriptor.IsEntityType()
                            ? TypeNames.EntityId
                            : typeReferenceDescriptor.Name);
                    break;
                case NonNullTypeDescriptor nonNullTypeDescriptor:
                    ToEntityIdBuilder(
                        nonNullTypeDescriptor.InnerType,
                        actualBuilder,
                        true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(typeReferenceDescriptor));
            }

            return actualBuilder;
        }
    }
}
