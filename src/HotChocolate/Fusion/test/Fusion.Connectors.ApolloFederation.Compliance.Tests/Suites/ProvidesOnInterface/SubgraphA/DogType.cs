using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphA;

/// <summary>
/// Apollo Federation descriptor for the <c>Dog</c> entity in
/// <c>subgraph-a</c>. Only declares the key field; name is owned
/// by subgraph-c.
/// </summary>
public sealed class DogType : ObjectType<Dog>
{
    protected override void Configure(IObjectTypeDescriptor<Dog> descriptor)
    {
        descriptor
            .Implements<AnimalInterfaceType>()
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(d => d.Id).Type<NonNullType<IdType>>();
    }

    private static Dog? ResolveById(string id)
        => new Dog { Id = id };
}
