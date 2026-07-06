using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.NonResolvableInterfaceObject.A;

public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("a")
            .Type<NodeType>()
            .Resolve(_ => new NodeImpl { Id = AData.NodeId });

        descriptor
            .Field("product")
            .Type<NonNullType<ProductType>>()
            .Resolve(_ => AData.FeaturedProduct);
    }
}
