using System;
using System.Collections.Generic;
using System.Text;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Sorting
{
    public interface ISortingNamingConvention : IConvention
    {
        NameString ArgumentName { get; }
        NameString SortKindAscName { get; }
        NameString SortKindDescName { get; }
    }
}
