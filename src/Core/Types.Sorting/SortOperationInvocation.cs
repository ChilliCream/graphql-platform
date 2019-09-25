using System;
using System.Reflection;

namespace HotChocolate.Types.Sorting
{
    public class SortOperationInvocation
    {
        public SortOperationInvocation(
            SortOperationKind kind,
            PropertyInfo property)
        {
            Kind = kind;
            Property = property
                ?? throw new ArgumentNullException(nameof(property));
        }

        public SortOperationKind Kind { get; }

        public PropertyInfo Property { get; }
    }
}
