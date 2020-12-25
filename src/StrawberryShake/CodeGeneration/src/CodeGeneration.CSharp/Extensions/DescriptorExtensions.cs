using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp.Extensions
{
    public static class DescriptorExtensions
    {
        public static TypeReferenceBuilder ToBuilder(this TypeReferenceDescriptor typeReferenceDescriptor) =>
            new TypeReferenceBuilder()
                .SetName(typeReferenceDescriptor.Name)
                .SetIsNullable(typeReferenceDescriptor.IsNullable)
                .SetListType(typeReferenceDescriptor.ListType);

        public static TypeReferenceBuilder ToEntityIdBuilder(this TypeReferenceDescriptor typeReferenceDescriptor) =>
            new TypeReferenceBuilder()
                .SetName(typeReferenceDescriptor.IsReferenceType ? WellKnownNames.EntityId : typeReferenceDescriptor.Name)
                .SetIsNullable(typeReferenceDescriptor.IsNullable)
                .SetListType(typeReferenceDescriptor.ListType);
    }
}
