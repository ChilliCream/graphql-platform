using System;
using System.Collections.Generic;
using System.Reflection;

namespace HotChocolate.Types.Sorting
{
    public class SortOperation
    {
        public SortOperation(PropertyInfo property)
        {
            Property = property
                       ?? throw new ArgumentNullException(nameof(property));
        }

        public PropertyInfo Property { get; }
    }
}
