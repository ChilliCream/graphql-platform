using HotChocolate.Types;

namespace HotChocolate.Lodash
{
    public class SumByDirectiveType : AggregationDirectiveType<SumByDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<SumByDirective> descriptor)
        {
            descriptor.Name("sumBy");
            descriptor.Location(DefaultDirectiveLocation);
            descriptor.Repeatable();
            descriptor.Argument(x => x.Key);
        }

        protected override AggregationOperation CreateOperation(SumByDirective directive)
        {
            return new SumByOperation(directive.Key);
        }
    }
}
