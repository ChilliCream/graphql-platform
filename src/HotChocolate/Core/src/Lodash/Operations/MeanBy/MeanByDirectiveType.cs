using HotChocolate.Types;

namespace HotChocolate.Lodash
{
    public class MeanByDirectiveType : AggregationDirectiveType<MeanByDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<MeanByDirective> descriptor)
        {
            descriptor.Name("meanBy");
            descriptor.Location(DefaultDirectiveLocation);
            descriptor.Repeatable();
            descriptor.Argument(x => x.Key);
        }

        protected override AggregationOperation CreateOperation(MeanByDirective directive)
        {
            return new MeanByOperation(directive.Key);
        }
    }
}
