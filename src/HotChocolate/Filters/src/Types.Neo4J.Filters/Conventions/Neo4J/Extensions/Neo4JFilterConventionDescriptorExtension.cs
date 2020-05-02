using HotChocolate.Types.Filters.Conventions;

namespace HotChocolate.Types.Neo4J.Filters.Conventions
{
    public static class Neo4JFilterConventionDescriptorExtension
    {
        public static INeo4JFilterVisitorDescriptor UseNeo4JVisitor(
            this IFilterConventionDescriptor descriptor)
        {
            var desc = Neo4JFilterVisitorDescriptor.New(descriptor);
            descriptor.Visitor(desc);
            return desc;
        }
    }
}
