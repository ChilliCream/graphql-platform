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
        private int _aliasCount;

        public SqlKataFilterVisitorContext(IFilterInputType initialType)
            : base(initialType)
        {
            RuntimeTypes = new Stack<IExtendedType>();
            RuntimeTypes.Push(initialType.EntityType);
        }

        /// <summary>
        /// The already visited runtime types
        /// </summary>
        public Stack<IExtendedType> RuntimeTypes { get; }

        /// <inheritdoc />
        public override FilterScope<Query> CreateScope()
        {
            SqlKataFilterScope scope = new();
            scope.Instance.Push(new Query());
            return scope;
        }

        public string GetTableAlias()
        {
            _aliasCount++;
            return _aliasCount + "_v";
        }
    }
}
