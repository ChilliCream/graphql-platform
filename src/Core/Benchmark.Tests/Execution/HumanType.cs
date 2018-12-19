using HotChocolate.Types;

namespace HotChocolate.Benchmark.Tests.Execution
{
    public class HumanType
        : ObjectType<Human>
    {
        protected override void Configure(IObjectTypeDescriptor<Human> descriptor)
        {
            descriptor.Interface<CharacterType>();
            descriptor.Field(t => t.Friends)
                .Type<ListType<CharacterType>>()
                .Resolver(c => CharacterType.GetCharacter(c));
            descriptor.Field(t => t.Height)
                .Argument("unit", a => a.Type<EnumType<Unit>>())
                .Resolver(c => CharacterType.GetHeight(c));
        }
    }

}
