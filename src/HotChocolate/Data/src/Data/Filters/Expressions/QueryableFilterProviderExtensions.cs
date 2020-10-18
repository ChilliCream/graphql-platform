using System;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableFilterProviderExtensions
        : FilterProviderExtensions<QueryableFilterContext>
    {
        public QueryableFilterProviderExtensions()
        {
        }

        public QueryableFilterProviderExtensions(
            Action<IFilterProviderDescriptor<QueryableFilterContext>> configure)
            : base(configure)
        {
        }
    }
}
