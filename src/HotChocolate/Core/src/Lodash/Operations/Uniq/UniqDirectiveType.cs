using HotChocolate.Types;

namespace HotChocolate.Lodash
{
    public class UniqDirectiveType : AggregationDirectiveType<UniqDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<UniqDirective> descriptor)
        {
            descriptor.Name("uniq");
            descriptor.Location(DefaultDirectiveLocation);
            descriptor.Repeatable();
        }

        protected override AggregationOperation CreateOperation(UniqDirective directive)
        {
            return new UniqOperation();
        }
    }
}
