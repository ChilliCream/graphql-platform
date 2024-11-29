using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.ApolloFederation.CertificationSchema.CodeFirst.Types;

public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .ExtendServiceType()
            .Name("Query")
            .Field("product")
            .Argument("id", a => a.Type<NonNullType<IdType>>())
            .Type<ProductType>()
            .Resolve(ctx =>
            {
                var id = ctx.ArgumentValue<string>("id");
                return ctx.Service<Data>().Products.FirstOrDefault(t => t.Id.Equals(id));
            });
    }
}
