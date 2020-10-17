using System;

namespace HotChocolate.Data.Sorting.Expressions
{
    public class QueryableSortProviderExtension
        : SortProviderExtensions<QueryableSortContext>
    {
        public QueryableSortProviderExtension()
        {
        }

        public QueryableSortProviderExtension(
            Action<ISortProviderDescriptor<QueryableSortContext>> configure) : base(configure)
        {
        }
    }
}
