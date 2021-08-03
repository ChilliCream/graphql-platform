using System.Collections.Generic;
using HotChocolate.Data.Filters;
using HotChocolate.Internal;
using SqlKata;

namespace HotChocolate.Data.SqlKata.Filters
{
    /// <inheritdoc />
    public class SqlKataFilterVisitorContext
        : FilterVisitorContext<Query>
    {
        public SqlKataFilterVisitorContext(IFilterInputType initialType)
            : base(initialType)
        {
            RuntimeTypes = new Stack<IExtendedType>();
            RuntimeTypes.Push(initialType.EntityType);
            Query = new Query();
        }

        /// <summary>
        /// The already visited runtime types
        /// </summary>
        public Stack<IExtendedType> RuntimeTypes { get; }

        public Query Query { get; }

        /// <inheritdoc />
        public override FilterScope<Query> CreateScope() => new SqlKataFilterScope();
    }
}
