using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphB;

/// <summary>
/// Descriptor for <c>Cat</c> in <c>subgraph-b</c>. Not keyed;
/// fields are shareable so the composition can merge with subgraph-c.
/// </summary>
public sealed class CatType : ObjectType<Cat>
{
    protected override void Configure(IObjectTypeDescriptor<Cat> descriptor)
    {
        descriptor.Implements<AnimalInterfaceType>();

        descriptor.Field(c => c.Id).Shareable().Type<NonNullType<IdType>>();
        descriptor.Field(c => c.Name).Shareable().Type<StringType>();
    }
}
