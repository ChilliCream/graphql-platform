using HotChocolate.Types.Filters.Conventions;

namespace HotChocolate.Types
{
    public class SelectionMiddlewareContext
    {
        private SelectionMiddlewareContext(
            IFilterConvention fiterConvention,
            string sortingArgumentName)
        {
            FilterConvention = fiterConvention;
            SortingArgumentName = sortingArgumentName;
        }

        public IFilterConvention FilterConvention { get; }

        public string SortingArgumentName { get; }

        public static SelectionMiddlewareContext Create(
            IFilterConvention fiterConvention,
            string sortingArgumentName) =>
                new SelectionMiddlewareContext(fiterConvention, sortingArgumentName);
    }
}
