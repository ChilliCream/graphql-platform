using System;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp.Extensions
{
    public static class DescriptorExtensions
    {
        public static ITypeReferenceBuilder ToBuilder(
            this ITypeDescriptor typeReferenceDescriptor,
            string? nameOverride = null)
        {
            return typeReferenceDescriptor switch
            {
                NonNullTypeDescriptor nonNullTypeDescriptor => new NonNullTypeReferenceBuilder()
                    .SetInnerType(nonNullTypeDescriptor.InnerType.ToBuilder(nameOverride)),
                ListTypeDescriptor listTypeDescriptor => new ListTypeReferenceBuilder()
                    .SetListType(listTypeDescriptor.InnerType.ToBuilder(nameOverride)),
                NamedTypeDescriptor namedTypeDescriptor => new TypeReferenceBuilder()
                    .SetName(nameOverride ?? namedTypeDescriptor.Name),
                _ => throw new NotSupportedException()
            };
        }

        public static ITypeReferenceBuilder ToEntityIdBuilder(
            this ITypeDescriptor typeReferenceDescriptor)
        {
            return typeReferenceDescriptor switch
            {
                NonNullTypeDescriptor nonNullTypeDescriptor => new NonNullTypeReferenceBuilder()
                    .SetInnerType(nonNullTypeDescriptor.InnerType.ToEntityIdBuilder()),
                ListTypeDescriptor listTypeDescriptor => new ListTypeReferenceBuilder()
                    .SetListType(listTypeDescriptor.InnerType.ToEntityIdBuilder()),
                NamedTypeDescriptor namedTypeDescriptor => new TypeReferenceBuilder()
                    .SetName(typeReferenceDescriptor.IsEntityType()
                        ? WellKnownNames.EntityId
                        : typeReferenceDescriptor.Name),
                _ => throw new NotSupportedException()
            };
        }
    }
}
