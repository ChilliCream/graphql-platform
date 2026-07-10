using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphC;

/// <summary>
/// Apollo Federation descriptor for the <c>Cat</c> entity in
/// <c>subgraph-c</c>. Keyed by <c>id</c>, with shareable <c>name</c>
/// and owning <c>age</c>.
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
        descriptor.Field(c => c.Name).Shareable().Type<StringType>();
        descriptor.Field(c => c.Age).Type<IntType>();
    }

    private static Cat? ResolveById(string id)
        => SubgraphCData.CatsById.TryGetValue(id, out var cat) ? cat : null;
}
