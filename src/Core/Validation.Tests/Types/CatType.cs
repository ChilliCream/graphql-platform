using HotChocolate.Types;

namespace HotChocolate.Validation
{
    public class CatType
        : ObjectType<Cat>
    {
        protected override void Configure(IObjectTypeDescriptor<Cat> descriptor)
        {
            descriptor.Interface<PetType>();
            descriptor.Field(t => t.Name)
                .Type<NonNullType<StringType>>();
            descriptor.Field(t => t.DoesKnowCommand(default))
                .Argument("catCommand", a => a.Type<NonNullType<EnumType<CatCommand>>>());
        }
    }
}
