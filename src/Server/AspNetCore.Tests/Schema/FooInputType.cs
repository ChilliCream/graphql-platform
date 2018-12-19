using HotChocolate.Types;

namespace HotChocolate.AspNetCore
{
    public class FooInputType
        : InputObjectType<Foo>
    {
        protected override void Configure(IInputObjectTypeDescriptor<Foo> descriptor)
        {
            descriptor.Name("FooInput");
        }
    }
}
