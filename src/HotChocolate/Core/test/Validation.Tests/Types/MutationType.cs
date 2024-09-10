using HotChocolate.Types;

namespace HotChocolate.Validation.Types;

public class MutationType
    : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name("Mutation");

        descriptor.Field("fieldB")
            .Type<NonNullType<StringType>>()
            .Resolve(() => "foo");

        descriptor.Field("addPet")
            .Argument("pet", a => a.Type<PetInputType>())
            .Type<PetType>()
            .Resolve(() => "foo");
    }
}
