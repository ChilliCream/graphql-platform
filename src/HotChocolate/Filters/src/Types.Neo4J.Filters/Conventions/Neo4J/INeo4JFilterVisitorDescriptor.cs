using HotChocolate.Types.Filters.Conventions;

namespace HotChocolate.Types.Neo4J.Filters.Conventions
{
    public interface INeo4JFilterVisitorDescriptor
        : IFilterVisitorDescriptor
    {
        IFilterConventionDescriptor And();
    }
}
