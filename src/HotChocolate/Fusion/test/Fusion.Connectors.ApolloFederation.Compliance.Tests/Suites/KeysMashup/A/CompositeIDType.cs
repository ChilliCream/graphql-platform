using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.KeysMashup.A;

/// <summary>
/// Descriptor for the <c>CompositeID</c> value type in subgraph <c>a</c>.
/// </summary>
public sealed class CompositeIDType : ObjectType<CompositeID>
{
    protected override void Configure(IObjectTypeDescriptor<CompositeID> descriptor)
    {
        descriptor.Name("CompositeID");
        descriptor.Field(c => c.One).Type<NonNullType<IdType>>();
        descriptor.Field(c => c.Two).Type<NonNullType<IdType>>();
        descriptor.Field(c => c.Three).Type<NonNullType<IdType>>();
    }
}
