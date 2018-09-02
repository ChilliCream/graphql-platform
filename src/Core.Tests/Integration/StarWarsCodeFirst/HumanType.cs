using HotChocolate.Types;

namespace HotChocolate.Integration.StarWarsCodeFirst
{
    public class HumanType
        : ObjectType<Human>
    {
        protected override void Configure(IObjectTypeDescriptor<Human> descriptor)
        {
            descriptor.Interface<CharacterType>();
            descriptor.Field<CommonResolvers>(r => r.GetCharacter(default, default))
                .Type<ListType<CharacterType>>()
                .Name("friends");
            descriptor.Field<CommonResolvers>(t => t.GetHeight(default, default))
                .Argument("unit", a => a.Type<EnumType<Unit>>())
                .Name("height");
        }
    }

}
