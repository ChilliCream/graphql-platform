using System.Collections.Generic;
using HotChocolate.Types.Filters.Conventions;

namespace HotChocolate.Types
{
    public class SelectionMiddlewareContext
    {
        private SelectionMiddlewareContext(
            IFilterConvention filterConvention,
            string sortingArgumentName)
        {
            FilterConvention = filterConvention;
            SortingArgumentName = sortingArgumentName;
        }

        public IFilterConvention FilterConvention { get; }

        public string SortingArgumentName { get; }

        public IList<IError> Errors { get; } =
            new List<IError>();

        public static SelectionMiddlewareContext Create(
            IFilterConvention filterConvention,
            string sortingArgumentName) =>
            new SelectionMiddlewareContext(filterConvention, sortingArgumentName);
    }
}
