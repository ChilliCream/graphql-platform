using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphA;

/// <summary>
/// Root <c>Query</c> for <c>subgraph-a</c>. Exposes
/// <c>media: Media @shareable</c> and <c>book: Book</c>.
/// </summary>
/// <remarks>
/// The original audit SDL has <c>@provides(fields: "animals { ... on Dog { name } }")</c>
/// on <c>Query.book</c>, but HC composition does not support <c>@provides</c>
/// through list-typed fields (SelectionSetValidator.NullableType does not
/// unwrap list wrappers). The <c>@provides</c> is omitted; the gateway
/// resolves animal names via entity calls to subgraph-c instead.
/// </remarks>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("media")
            .Type<MediaInterfaceType>()
            .Shareable()
            .Resolve(_ =>
            {
                var animals = SubgraphAData.BookAnimalIds["m1"]
                    .Select<string, IAnimal>(id =>
                    {
                        var type = SubgraphAData.AnimalTypes.GetValueOrDefault(id, "Dog");
                        return type == "Cat"
                            ? new Cat { Id = id }
                            : (IAnimal)new Dog { Id = id };
                    })
                    .ToList();

                return new Book { Id = "m1", Animals = animals };
            });

        descriptor
            .Field("book")
            .Type<BookType>()
            .Resolve(_ =>
            {
                var animals = SubgraphAData.BookAnimalIds["m1"]
                    .Select<string, IAnimal>(id =>
                    {
                        var type = SubgraphAData.AnimalTypes.GetValueOrDefault(id, "Dog");
                        return type == "Cat"
                            ? new Cat { Id = id }
                            : (IAnimal)new Dog { Id = id };
                    })
                    .ToList();

                return new Book { Id = "m1", Animals = animals };
            });
    }
}
