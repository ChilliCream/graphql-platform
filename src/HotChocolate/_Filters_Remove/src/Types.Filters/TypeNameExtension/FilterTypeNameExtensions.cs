using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Types.Filters
{
    public static class FilterTypeNameExtensions
    {
        public static IFilterInputTypeNameDependencyDescriptor<T> Name<T>(
          this IFilterInputTypeDescriptor<T> descriptor,
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

            return new FilterInputTypeNameDependencyDescriptor<T>(
                descriptor, createName);
        }
    }
}
