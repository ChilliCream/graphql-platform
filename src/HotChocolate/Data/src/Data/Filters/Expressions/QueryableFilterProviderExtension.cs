using System;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableFilterProviderExtension
        : FilterProviderExtensions<QueryableFilterContext>
    {
        public QueryableFilterProviderExtension()
        {
        }

        public QueryableFilterProviderExtension(
            Action<IFilterProviderDescriptor<QueryableFilterContext>> configure)
            : base(configure)
        {
        }
    }
}
