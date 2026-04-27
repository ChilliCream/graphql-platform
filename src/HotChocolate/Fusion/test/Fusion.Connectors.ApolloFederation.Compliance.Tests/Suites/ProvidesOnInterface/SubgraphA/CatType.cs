using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphA;

/// <summary>
/// Apollo Federation descriptor for the <c>Cat</c> entity in
/// <c>subgraph-a</c>. Only declares the key field; name and age
/// are owned by subgraph-c.
/// </summary>
public sealed class CatType : ObjectType<Cat>
{
    protected override void Configure(IObjectTypeDescriptor<Cat> descriptor)
    {
        descriptor
            .Implements<AnimalInterfaceType>()
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(c => c.Id).Type<NonNullType<IdType>>();
    }

    private static Cat? ResolveById(string id)
        => new Cat { Id = id };
}
