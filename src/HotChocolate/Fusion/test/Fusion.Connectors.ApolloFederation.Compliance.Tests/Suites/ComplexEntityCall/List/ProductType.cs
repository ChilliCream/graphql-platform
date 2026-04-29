using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ComplexEntityCall.List;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity owned by the
/// <c>list</c> subgraph (<c>@key(fields: "id pid")</c>).
/// </summary>
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Key("id pid")
            .ResolveReferenceWith(_ => ResolveByIdPid(default!, default));

        descriptor.Field(p => p.Id).Type<NonNullType<StringType>>();
        descriptor.Field(p => p.Pid).Type<StringType>();
    }

    private static Product ResolveByIdPid(string id, string? pid)
        => ListData.ResolveIdPid(id, pid);
}
