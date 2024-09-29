using HotChocolate.Types;

namespace HotChocolate.Data.Filters;

public static class FilterTypeNameExtensions
{
    public static IFilterInputTypeNameDependencyDescriptor<T> Name<T>(
        this IFilterInputTypeDescriptor<T> descriptor,
        Func<INamedType, string> createName)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(createName);

        return new FilterInputTypeNameDependencyDescriptor<T>(descriptor, createName);
    }
}
