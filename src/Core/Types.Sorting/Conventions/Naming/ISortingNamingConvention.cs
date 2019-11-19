using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Sorting
{
    public interface ISortingNamingConvention : IConvention
    {
        NameString ArgumentName { get; }
        NameString SortKindAscName { get; }
        NameString SortKindDescName { get; }
    }
}
