using HotChocolate.Types.Filters.Conventions;

namespace HotChocolate.Types.Neo4J.Filters.Conventions
{
    public class Neo4JFilterVisitorDescriptor :
        FilterVisitorDescriptorBase<Neo4JFilterVisitorDefinition>,
        INeo4JFilterVisitorDescriptor
    {
        private readonly IFilterConventionDescriptor _convention;

        protected Neo4JFilterVisitorDescriptor(
            IFilterConventionDescriptor convention)
        {
            _convention = convention;
        }

        protected override Neo4JFilterVisitorDefinition Definition { get; } =
            new Neo4JFilterVisitorDefinition();

        public IFilterConventionDescriptor And() => _convention;

        public override Neo4JFilterVisitorDefinition CreateDefinition()
        {
            return Definition;
        }

        public static Neo4JFilterVisitorDescriptor New(
            IFilterConventionDescriptor convention) =>
                new Neo4JFilterVisitorDescriptor(convention);
    }
}
