using HotChocolate.Types;

namespace HotChocolate.Lodash
{
    public class KeyByDirectiveType : AggregationDirectiveType<KeyByDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<KeyByDirective> descriptor)
        {
            descriptor.Name("keyBy");
            descriptor.Location(DefaultDirectiveLocation);
            descriptor.Repeatable();
            descriptor.Argument(x => x.Key);
        }

        protected override AggregationOperation CreateOperation(KeyByDirective directive)
        {
            return new KeyByOperation(directive.Key);
        }
    }
}
