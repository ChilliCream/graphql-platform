using HotChocolate.Types;

namespace Products;

public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Name("Query")
            .Field("topProducts")
            .Argument("first", a => a.Type<IntType>())
            .Type<NonNullType<ListType<NonNullType<ProductType>>>>()
            .Resolve(ctx => ctx.Service<ProductRepository>().GetTop(
                ctx.ArgumentValue<int?>("first") ?? 5));
    }
}
