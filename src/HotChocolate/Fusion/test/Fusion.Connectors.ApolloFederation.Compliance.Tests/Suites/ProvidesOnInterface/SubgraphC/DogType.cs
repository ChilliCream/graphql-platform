using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphC;

/// <summary>
/// Apollo Federation descriptor for the <c>Dog</c> entity in
/// <c>subgraph-c</c>. Keyed by <c>id</c>, with shareable <c>name</c>
/// and owning <c>age</c>.
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
        descriptor.Field(d => d.Name).Shareable().Type<StringType>();
        descriptor.Field(d => d.Age).Type<IntType>();
    }

    private static Dog? ResolveById(string id)
        => SubgraphCData.DogsById.TryGetValue(id, out var dog) ? dog : null;
}
