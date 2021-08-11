using HotChocolate.Types;

namespace HotChocolate.Lodash
{
    public class MaxByDirectiveType : AggregationDirectiveType<MaxByDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<MaxByDirective> descriptor)
        {
            descriptor.Name("maxBy");
            descriptor.Location(DefaultDirectiveLocation);
            descriptor.Repeatable();
            descriptor.Argument(x => x.Key);
        }

        protected override AggregationOperation CreateOperation(MaxByDirective directive)
        {
            return new MaxByOperation(directive.Key);
        }
    }
}
