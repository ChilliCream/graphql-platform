using System.Collections.Generic;
using HotChocolate.Data.Filters;
using HotChocolate.Internal;

namespace HotChocolate.Data.MongoDb.Filters
{
    /// <inheritdoc />
    public class MongoDbFilterVisitorContext
        : FilterVisitorContext<MongoDbFilterDefinition>
    {
        public MongoDbFilterVisitorContext(IFilterInputType initialType)
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
        public override FilterScope<MongoDbFilterDefinition> CreateScope() =>
            new MongoDbFilterScope();
    }
}
