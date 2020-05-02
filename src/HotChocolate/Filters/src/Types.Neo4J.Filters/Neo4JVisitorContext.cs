using System;
using HotChocolate.Types.Filters;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Neo4J.Filters
{
    public class Neo4JFilterVisitorContext
        : FilterVisitorContextBase, INeo4JFilterVisitorContext
    {
        public Neo4JFilterVisitorContext(
            InputObjectType initialType,
            Type source,
            ITypeConversion typeConverter)
            : base(initialType)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            TypeConverter = typeConverter ??
                throw new ArgumentNullException(nameof(typeConverter));
        }

        public ITypeConversion TypeConverter { get; }
    }
}
