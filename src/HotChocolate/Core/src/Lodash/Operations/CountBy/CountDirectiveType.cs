using HotChocolate.Types;

namespace HotChocolate.Lodash
{
    public class CountDirectiveType : AggregationDirectiveType<CountDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<CountDirective> descriptor)
        {
            descriptor.Name("count");
            descriptor.Location(DefaultDirectiveLocation);
            descriptor.Repeatable();
            descriptor.Argument(x => x.By);
        }

        protected override AggregationOperation CreateOperation(CountDirective directive)
        {
            return new CountOperation(directive.By);
        }
    }
}
