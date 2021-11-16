using HotChocolate.Types;

namespace HotChocolate.Lodash
{
    public class UniqueDirectiveType : AggregationDirectiveType<UniqueDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<UniqueDirective> descriptor)
        {
            descriptor.Name("unique");
            descriptor.Location(DefaultDirectiveLocation);
            descriptor.Repeatable();
            descriptor.Argument(x => x.By);
        }

        protected override AggregationOperation CreateOperation(UniqueDirective directive)
        {
            return new UniqueOperation(directive.By);
        }
    }
}
