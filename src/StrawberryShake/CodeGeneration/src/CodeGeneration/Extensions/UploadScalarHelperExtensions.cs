using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.Mappers;

public static class UploadScalarHelperExtensions
{
    public static bool HasUpload(this ITypeDescriptor descriptor)
        => descriptor.NamedType()
            is InputObjectTypeDescriptor { HasUpload: true, }
            or ScalarTypeDescriptor { Name: "Upload", };
}
