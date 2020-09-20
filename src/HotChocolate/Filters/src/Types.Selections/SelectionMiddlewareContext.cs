namespace HotChocolate.Types
{
    public class SelectionMiddlewareContext
    {
        private SelectionMiddlewareContext(
            string filterArgumentName,
            string sortingArgumentName)
        {
            FilterArgumentName = filterArgumentName;
            SortingArgumentName = sortingArgumentName;
        }

        public string FilterArgumentName { get; }

        public string SortingArgumentName { get; }

        public static SelectionMiddlewareContext Create(
            string filterArgumentName,
            string sortingArgumentName) =>
                new SelectionMiddlewareContext(filterArgumentName, sortingArgumentName);
    }
}
