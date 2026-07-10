using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.UnionInterfaceDistributed.A;

/// <summary>
/// Root <c>Query</c> for subgraph <c>a</c>. Exposes
/// <c>products</c>, <c>node(id: ID!)</c>, <c>nodes</c>, and <c>toasters</c>.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("products")
            .Type<ListType<ProductUnionType>>()
            .Resolve(_ => SubgraphAData.Products);

        descriptor
            .Field("node")
            .Argument("id", a => a.Type<NonNullType<IdType>>())
            .Type<NodeType>()
            .Resolve(ctx =>
            {
                var id = ctx.ArgumentValue<string>("id");

                // Only Toaster implements Node in subgraph A.
                if (SubgraphAData.ToastersById.TryGetValue(id, out var toaster))
                {
                    return (INode)toaster;
                }

                return null;
            });

        descriptor
            .Field("nodes")
            .Type<ListType<NodeType>>()
            .Resolve(_ => SubgraphAData.Toasters.Cast<INode>().ToList());

        descriptor
            .Field("toasters")
            .Type<ListType<ToasterType>>()
            .Resolve(_ => SubgraphAData.Toasters);
    }
}
