using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphA;

/// <summary>
/// Root <c>Query</c> for <c>subgraph-a</c>. Exposes
/// <c>media: Media @shareable</c> and <c>book: Book</c>.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("media")
            .Type<MediaInterfaceType>()
            .Shareable()
            .Resolve(_ => throw new InvalidOperationException(
                "You should be using the 'a' subgraph!"));

        descriptor
            .Field("book")
            .Type<BookType>()
            .Provides("animals { ... on Dog { name } }")
            .Resolve(_ =>
            {
                var animals = SubgraphAData.BookAnimalIds["m1"]
                    .Select<string, IAnimal>(id =>
                    {
                        var type = SubgraphAData.AnimalTypes.GetValueOrDefault(id, "Dog");
                        return type == "Cat"
                            ? new Cat { Id = id }
                            : (IAnimal)new Dog { Id = id, Name = "Fido" };
                    })
                    .ToList();

                return new Book { Id = "m1", Animals = animals };
            });
    }
}
