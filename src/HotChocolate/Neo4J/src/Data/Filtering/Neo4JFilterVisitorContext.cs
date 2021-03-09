using System.Collections.Generic;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Internal;

namespace HotChocolate.Data.Neo4J.Filtering
{
    /// <inheritdoc />
    public class Neo4JFilterVisitorContext
        : FilterVisitorContext<Condition>
    {
        public Neo4JFilterVisitorContext(IFilterInputType initialType)
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
        public override FilterScope<Condition> CreateScope() =>
            new Neo4JFilterScope();
    }

}
