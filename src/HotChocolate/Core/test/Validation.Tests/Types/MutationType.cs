using HotChocolate.Types;

namespace HotChocolate.Validation
{
    public class MutationType
        : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Mutation");

            descriptor.Field("fieldB")
                .Type<NonNullType<StringType>>()
                .Resolver(() => "foo");
        }
    }
}
