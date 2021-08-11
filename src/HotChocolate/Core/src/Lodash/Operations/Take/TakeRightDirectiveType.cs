using HotChocolate.Types;

namespace HotChocolate.Lodash
{
    public class TakeRightDirectiveType : AggregationDirectiveType<TakeRightDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<TakeRightDirective> descriptor)
        {
            descriptor.Name("takeRight");
            descriptor.Location(DefaultDirectiveLocation);
            descriptor.Repeatable();
            descriptor.Argument(x => x.Count);
        }

        protected override AggregationOperation CreateOperation(TakeRightDirective directive)
        {
            return new TakeRightOperation(directive.Count);
        }
    }
}
