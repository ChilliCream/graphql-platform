using HotChocolate.Types;

namespace HotChocolate.Data.Sorting;

public static class SortTypeNameExtensions
{
    public static ISortInputTypeNameDependencyDescriptor<T> Name<T>(
        this ISortInputTypeDescriptor<T> descriptor,
        Func<ITypeDefinition, string> createName)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(createName);

        return new SortInputTypeNameDependencyDescriptor<T>(descriptor, createName);
    }
}
