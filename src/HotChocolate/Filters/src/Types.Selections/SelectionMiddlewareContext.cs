using HotChocolate.Types.Sorting.Conventions;

namespace HotChocolate.Types
{
    public class SelectionMiddlewareContext
    {
        private SelectionMiddlewareContext(
            string filterArgumentName,
            ISortingConvention sortingConvention)
        {
            FilterArgumentName = filterArgumentName;
            SortingConvention = sortingConvention;
        }

        public string FilterArgumentName { get; }

        public ISortingConvention SortingConvention { get; }

        public static SelectionMiddlewareContext Create(
            string filterArgumentName,
            ISortingConvention sortingConvention) =>
                new SelectionMiddlewareContext(filterArgumentName, sortingConvention);
    }
}
