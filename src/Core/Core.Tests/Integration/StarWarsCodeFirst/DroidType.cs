using HotChocolate.Types;

namespace HotChocolate.Integration.StarWarsCodeFirst
{
    public class DroidType
        : ObjectType<Droid>
    {
        protected override void Configure(IObjectTypeDescriptor<Droid> descriptor)
        {
            descriptor.Interface<CharacterType>();

            descriptor.Field(t => t.Id).Type<NonNullType<StringType>>();

            descriptor.Field(t => t.AppearsIn).Type<ListType<EpisodeType>>();

            descriptor.Field<CommonResolvers>(r => r.GetCharacter(default, default))
                .Type<ListType<CharacterType>>()
                .Name("friends");

            descriptor.Field<CommonResolvers>(t => t.GetHeight(default, default))
                .Type<FloatType>()
                .Argument("unit", a => a.Type<EnumType<Unit>>())
                .Name("height");
        }
    }

}
