using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphB;

/// <summary>
/// Descriptor for <c>Dog</c> in <c>subgraph-b</c>. Not keyed;
/// fields are shareable so the composition can merge with subgraph-c.
/// </summary>
public sealed class DogType : ObjectType<Dog>
{
    protected override void Configure(IObjectTypeDescriptor<Dog> descriptor)
    {
        descriptor.Implements<AnimalInterfaceType>();

        descriptor.Field(d => d.Id).Shareable().Type<NonNullType<IdType>>();
        descriptor.Field(d => d.Name).Shareable().Type<StringType>();
    }
}
