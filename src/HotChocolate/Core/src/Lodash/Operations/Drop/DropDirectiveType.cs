using HotChocolate.Types;

namespace HotChocolate.Lodash
{
    public class DropDirectiveType : AggregationDirectiveType<DropDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<DropDirective> descriptor)
        {
            descriptor.Name("drop");
            descriptor.Location(DefaultDirectiveLocation);
            descriptor.Repeatable();
            descriptor.Argument(x => x.Count);
        }

        protected override AggregationOperation CreateOperation(DropDirective directive)
        {
            return new DropOperation(directive.Count);
        }
    }
}
