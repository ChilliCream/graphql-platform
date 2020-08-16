using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Types.Sorting
{
    public static class SortTypeNameExtensions
    {
        public static ISortInputTypeNameDependencyDescriptor<T> Name<T>(
          this ISortInputTypeDescriptor<T> descriptor,
          Func<INamedType, NameString> createName)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (createName == null)
            {
                throw new ArgumentNullException(nameof(createName));
            }

            return new SortInputTypeNameDependencyDescriptor<T>(
                descriptor, createName);
        }
    }
}
