using HotChocolate.Types.Sorting.Conventions;

namespace HotChocolate.Types.Sorting
{
    public class SortMiddlewareContext
    {
        public SortMiddlewareContext(ISortingConvention convetion)
        {
            Convention = convetion;
        }

        public ISortingConvention Convention { get; }

        public static SortMiddlewareContext Create(ISortingConvention convention)
        {
            return new SortMiddlewareContext(convention);
        }
    }
}
