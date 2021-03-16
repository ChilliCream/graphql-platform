using System;

namespace HotChocolate.Types.Filters
{
    [Obsolete("Use HotChocolate.Data.")]
    public static class FilterTypeNameExtensions
    {
        [Obsolete("Use HotChocolate.Data.")]
        public static IFilterInputTypeNameDependencyDescriptor<T> Name<T>(
          this IFilterInputTypeDescriptor<T> descriptor,
          Func<INamedType, NameString> createName)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (createName is null)
            {
                throw new ArgumentNullException(nameof(createName));
            }

            return new FilterInputTypeNameDependencyDescriptor<T>(
                descriptor, createName);
        }
    }
}
