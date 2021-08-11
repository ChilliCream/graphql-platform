using HotChocolate.Types;

namespace HotChocolate.Lodash
{
    public class TakeDirectiveType : AggregationDirectiveType<TakeDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<TakeDirective> descriptor)
        {
            descriptor.Name("take");
            descriptor.Location(DefaultDirectiveLocation);
            descriptor.Repeatable();
            descriptor.Argument(x => x.Count);
        }

        protected override AggregationOperation CreateOperation(TakeDirective directive)
        {
            return new TakeOperation(directive.Count);
        }
    }
}
