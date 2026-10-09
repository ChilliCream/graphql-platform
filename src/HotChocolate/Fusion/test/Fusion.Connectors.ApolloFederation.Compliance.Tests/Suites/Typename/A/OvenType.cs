using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Typename.A;

/// <summary>
/// Apollo Federation descriptor for the <c>Oven</c> concrete type owned by
/// the <c>a</c> subgraph (<c>type Oven implements Node { id: ID! }</c>).
/// </summary>
public sealed class OvenType : ObjectType<Oven>
{
    protected override void Configure(IObjectTypeDescriptor<Oven> descriptor)
    {
        descriptor.Implements<NodeType>();
        descriptor.Field(o => o.Id).Type<NonNullType<IdType>>();
    }
}
