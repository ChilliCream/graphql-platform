using HotChocolate.Types;

namespace HotChocolate.Lodash
{
    public class MaxDirectiveType : AggregationDirectiveType<MaxDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<MaxDirective> descriptor)
        {
            descriptor.Name("max");
            descriptor.Location(DefaultDirectiveLocation);
            descriptor.Repeatable();
            descriptor.Argument(x => x.By);
        }

        protected override AggregationOperation CreateOperation(MaxDirective directive)
        {
            return new MaxOperation(directive.By);
        }
    }
}
