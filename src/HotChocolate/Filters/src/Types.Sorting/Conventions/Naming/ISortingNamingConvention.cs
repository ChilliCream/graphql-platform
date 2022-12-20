using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Sorting;

[Obsolete("Use HotChocolate.Data.")]
public interface ISortingNamingConvention : IConvention
{
    string ArgumentName { get; }

    string SortKindAscName { get; }

    string SortKindDescName { get; }

    string GetSortingTypeName(IDescriptorContext context, Type entityType);

    string GetSortingOperationKindTypeName(IDescriptorContext context, Type entityType);
}
