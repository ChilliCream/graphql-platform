using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp.Extensions
{
    public static class DescriptorExtensions
    {
        public static TypeReferenceBuilder ToBuilder(
            this ITypeDescriptor typeReferenceDescriptor,
            string? nameOverride = null)
        {
            var ret = new TypeReferenceBuilder()
                .SetName(nameOverride ?? typeReferenceDescriptor.Name)
                .SetIsNullable(typeReferenceDescriptor.IsNullable);

            if (typeReferenceDescriptor is ListTypeDescriptor listTypeDescriptor)
            {
                ret.SetListType(listTypeDescriptor.InnerType.ToBuilder());
            }

            return ret;
        }

        public static TypeReferenceBuilder ToEntityIdBuilder(
            this ITypeDescriptor typeReferenceDescriptor)
        {
            var ret = new TypeReferenceBuilder()
                .SetName(
                    typeReferenceDescriptor.IsEntityType
                        ? WellKnownNames.EntityId
                        : typeReferenceDescriptor.Name)
                .SetIsNullable(typeReferenceDescriptor.IsNullable);

            if (typeReferenceDescriptor is ListTypeDescriptor listTypeDescriptor)
            {
                ret.SetListType(listTypeDescriptor.InnerType.ToBuilder());
            }

            return ret;
        }
    }
}
