using HotChocolate.Types;

namespace HotChocolate.Lodash
{
    public class FlattenDirectiveType : AggregationDirectiveType<FlattenDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<FlattenDirective> descriptor)
        {
            descriptor.Name("flatten");
            descriptor.Location(DefaultDirectiveLocation);
            descriptor.Repeatable();
            descriptor.Argument(x => x.Depth);
        }

        protected override AggregationOperation CreateOperation(FlattenDirective directive)
        {
            return new FlattenOperation(directive.Depth);
        }
    }
}
