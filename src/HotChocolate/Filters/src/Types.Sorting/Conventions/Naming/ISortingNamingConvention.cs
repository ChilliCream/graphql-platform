using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Sorting
{
    [Obsolete("Use HotChocolate.Data.")]
    public interface ISortingNamingConvention : IConvention
    {
        NameString ArgumentName { get; }

        NameString SortKindAscName { get; }

        NameString SortKindDescName { get; }

        NameString GetSortingTypeName(IDescriptorContext context, Type entityType);

        NameString GetSortingOperationKindTypeName(IDescriptorContext context, Type entityType);
    }
}
