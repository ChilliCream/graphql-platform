using HotChocolate.Types;

namespace HotChocolate.Lodash
{
    public class GroupByDirectiveType : AggregationDirectiveType<GroupByDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<GroupByDirective> descriptor)
        {
            descriptor.Name("groupBy");
            descriptor.Location(DefaultDirectiveLocation);
            descriptor.Repeatable();
            descriptor.Argument(x => x.Key);
        }

        protected override AggregationOperation CreateOperation(GroupByDirective directive)
        {
            return new GroupByOperation(directive.Key);
        }
    }
}
