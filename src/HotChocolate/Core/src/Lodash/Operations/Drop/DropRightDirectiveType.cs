using HotChocolate.Types;

namespace HotChocolate.Lodash
{
    public class DropRightDirectiveType : AggregationDirectiveType<DropRightDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<DropRightDirective> descriptor)
        {
            descriptor.Name("dropRight");
            descriptor.Location(DefaultDirectiveLocation);
            descriptor.Repeatable();
            descriptor.Argument(x => x.Count);
        }

        protected override AggregationOperation CreateOperation(DropRightDirective directive)
        {
            return new DropRightOperation(directive.Count);
        }
    }
}
