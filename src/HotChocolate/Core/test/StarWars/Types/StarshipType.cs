using HotChocolate.Types;
using HotChocolate.StarWars.Models;
using HotChocolate.StarWars.Resolvers;

namespace HotChocolate.StarWars.Types;

public class StarshipType
    : ObjectType<Starship>
{
    protected override void Configure(IObjectTypeDescriptor<Starship> descriptor)
    {
        descriptor.Field(t => t.Id)
            .Type<NonNullType<IdType>>();

        descriptor.Field(f => f.Name)
            .Type<NonNullType<StringType>>();

        descriptor.Field<SharedResolvers>(t => t.GetLength(default, default!));
    }
}
