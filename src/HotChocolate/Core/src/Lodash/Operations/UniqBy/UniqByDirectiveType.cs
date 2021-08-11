using HotChocolate.Types;

namespace HotChocolate.Lodash
{
    public class UniqByDirectiveType : AggregationDirectiveType<UniqByDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<UniqByDirective> descriptor)
        {
            descriptor.Name("uniqBy");
            descriptor.Location(DefaultDirectiveLocation);
            descriptor.Repeatable();
            descriptor.Argument(x => x.Key);
        }

        protected override AggregationOperation CreateOperation(UniqByDirective directive)
        {
            return new UniqByOperation(directive.Key);
        }
    }
}
