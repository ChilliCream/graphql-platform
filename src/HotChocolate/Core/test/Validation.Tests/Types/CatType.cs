using HotChocolate.Types;

namespace HotChocolate.Validation.Types;

public class CatType : ObjectType<Cat>
{
    protected override void Configure(IObjectTypeDescriptor<Cat> descriptor)
    {
        descriptor.Implements<PetType>();
        descriptor.Implements<BeingType>();
        descriptor.Implements<MammalType>();
        descriptor.Field(t => t.Name).Type<NonNullType<StringType>>();
        descriptor.Field(t => t.DoesKnowCommand(default))
            .Argument("catCommand", a => a.Type<NonNullType<EnumType<CatCommand>>>());
        descriptor.Field("furColor").Type<FurColor>().Resolve(() => null!);
    }
}

public class MammalType : InterfaceType
{
    protected override void Configure(IInterfaceTypeDescriptor descriptor)
    {
        descriptor.Name("Mammal");
        descriptor.Field("name").Type<NonNullType<StringType>>();
    }
}

public class CanineType : InterfaceType
{
    protected override void Configure(IInterfaceTypeDescriptor descriptor)
    {
        descriptor.Name("Canine");
        descriptor.Implements<MammalType>();
        descriptor.Field("name").Type<NonNullType<StringType>>();
    }
}
