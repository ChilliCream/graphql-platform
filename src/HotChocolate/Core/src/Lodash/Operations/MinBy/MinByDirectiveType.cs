using HotChocolate.Types;

namespace HotChocolate.Lodash
{
    public class MinByDirectiveType : AggregationDirectiveType<MinByDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<MinByDirective> descriptor)
        {
            descriptor.Name("minBy");
            descriptor.Location(DefaultDirectiveLocation);
            descriptor.Repeatable();
            descriptor.Argument(x => x.Key);
        }

        protected override AggregationOperation CreateOperation(MinByDirective directive)
        {
            return new MinByOperation(directive.Key);
        }
    }
}
