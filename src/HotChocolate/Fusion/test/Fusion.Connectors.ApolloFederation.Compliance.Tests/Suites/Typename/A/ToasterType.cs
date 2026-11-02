using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Typename.A;

/// <summary>
/// Apollo Federation descriptor for the <c>Toaster</c> concrete type owned
/// by the <c>a</c> subgraph
/// (<c>type Toaster implements Node { id: ID! }</c>).
/// </summary>
public sealed class ToasterType : ObjectType<Toaster>
{
    protected override void Configure(IObjectTypeDescriptor<Toaster> descriptor)
    {
        descriptor.Implements<NodeType>();
        descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();
    }
}
