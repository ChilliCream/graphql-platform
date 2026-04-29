using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.KeysMashup.B;

/// <summary>
/// Descriptor for the <c>CompositeID</c> value type in subgraph <c>b</c>.
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
