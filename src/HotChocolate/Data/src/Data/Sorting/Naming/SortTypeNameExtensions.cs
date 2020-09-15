using System;
using HotChocolate.Types;

namespace HotChocolate.Data.Sorting
{
    public static class SortTypeNameExtensions
    {
        public static ISortInputTypeNameDependencyDescriptor<T> Name<T>(
            this ISortInputTypeDescriptor<T> descriptor,
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

            return new SortInputTypeNameDependencyDescriptor<T>(descriptor, createName);
        }
    }
}