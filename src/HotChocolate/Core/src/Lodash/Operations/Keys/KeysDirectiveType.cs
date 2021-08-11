using HotChocolate.Types;

namespace HotChocolate.Lodash
{
    public class KeysDirectiveType : AggregationDirectiveType<KeysDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<KeysDirective> descriptor)
        {
            descriptor.Name("keys");
            descriptor.Location(DefaultDirectiveLocation);
            descriptor.Repeatable();
        }

        protected override AggregationOperation CreateOperation(KeysDirective directive)
        {
            return new KeysOperation();
        }
    }
}
