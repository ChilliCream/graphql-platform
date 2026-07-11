using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ComplexEntityCall.Link;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity owned by the
/// <c>link</c> subgraph. Resolves by <c>id</c> and by the composite
/// <c>id pid</c> key.
/// </summary>
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor
            .Key("id pid")
            .ResolveReferenceWith(_ => ResolveByIdAndPid(default!, default!));

        descriptor.Field(p => p.Id).Type<NonNullType<StringType>>();
        descriptor.Field(p => p.Pid).Type<NonNullType<StringType>>();
    }

    private static Product? ResolveById(string id)
        => LinkData.ById.TryGetValue(id, out var p) ? p : null;

    private static Product? ResolveByIdAndPid(string id, string pid)
        => LinkData.Items.FirstOrDefault(
            p => p.Id.Equals(id, StringComparison.Ordinal)
                && p.Pid.Equals(pid, StringComparison.Ordinal));
}
