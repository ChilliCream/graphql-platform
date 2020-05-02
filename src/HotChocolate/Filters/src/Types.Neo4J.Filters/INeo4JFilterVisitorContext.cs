using HotChocolate.Types.Filters;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Neo4J.Filters
{
    public interface INeo4JFilterVisitorContext
        : IFilterVisitorContextBase
    {
        ITypeConversion TypeConverter { get; }
    }
}
