using System;
using System.Collections.Generic;
using System.Reflection;

namespace HotChocolate.Types.Sorting
{
    public class SortOperation
    {
        public SortOperation(
            IEnumerable<SortOperationKind> allowedSorts,
            PropertyInfo property)
        {
            AllowedSorts = allowedSorts
                           ?? throw new ArgumentNullException(nameof(allowedSorts));
            Property = property
                       ?? throw new ArgumentNullException(nameof(property));
        }

        public IEnumerable<SortOperationKind> AllowedSorts { get; }

        public PropertyInfo Property { get; }
    }
}
