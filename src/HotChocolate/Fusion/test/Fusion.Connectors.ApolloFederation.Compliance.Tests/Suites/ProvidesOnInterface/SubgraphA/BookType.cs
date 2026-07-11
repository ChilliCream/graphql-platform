using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphA;

/// <summary>
/// Apollo Federation descriptor for the <c>Book</c> entity in
/// <c>subgraph-a</c>. Implements <c>Media</c>, keyed by <c>id</c>,
/// with a shareable <c>animals</c> field.
/// </summary>
public sealed class BookType : ObjectType<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor
            .Implements<MediaInterfaceType>()
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(b => b.Id).Type<NonNullType<IdType>>();

        descriptor
            .Field(b => b.Animals)
            .Shareable()
            .Type<ListType<AnimalInterfaceType>>();
    }

    private static Book? ResolveById(string id)
    {
        if (!SubgraphAData.BookAnimalIds.TryGetValue(id, out var animalIds))
        {
            return null;
        }

        var animals = animalIds
            .Select<string, IAnimal>(aid =>
            {
                var type = SubgraphAData.AnimalTypes.GetValueOrDefault(aid, "Dog");
                return type == "Cat"
                    ? new Cat { Id = aid }
                    : (IAnimal)new Dog { Id = aid };
            })
            .ToList();

        return new Book { Id = id, Animals = animals };
    }
}
