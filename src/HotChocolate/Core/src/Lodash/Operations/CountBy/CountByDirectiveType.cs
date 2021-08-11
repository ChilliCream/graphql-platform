using HotChocolate.Types;

namespace HotChocolate.Lodash
{
    public class CountByDirectiveType : AggregationDirectiveType<CountByDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<CountByDirective> descriptor)
        {
            descriptor.Name("countBy");
            descriptor.Location(DefaultDirectiveLocation);
            descriptor.Repeatable();
            descriptor.Argument(x => x.Key);
        }

        protected override AggregationOperation CreateOperation(CountByDirective directive)
        {
            return new CountByOperation(directive.Key);
        }
    }
}
